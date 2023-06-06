import socket
import sys

class FrameReader:

    def __init__(self, callback):
        self.callback = callback
        self.gathering_msg = False
        self.message_length = None
        self.message_bytes_processed = None
        self.message_buffer = None

        self.prefix_bytes_processed = 0
        self.prefix_buffer = bytearray(4)

    def process(self, data):
        bytes_processed = 0
        data_len = len(data)

        while bytes_processed != data_len:
            if not self.gathering_msg:
                prefix_bytes_remaining = 4 - self.prefix_bytes_processed
                bytes_remaining = data_len - bytes_processed
                readlength = min(bytes_remaining, prefix_bytes_remaining)

                p_start = self.prefix_bytes_processed
                p_end = p_start + readlength
                d_start = bytes_processed
                d_end = d_start + readlength

                if d_end > data_len:
                    raise Exception(f"Message framing error, processed {d_end} bytes for data length {data_len}")

                if p_end > 4:
                    raise Exception(f"Message framing error, processed {p_end} bytes for prefix of length {4}")

                self.prefix_buffer[p_start:p_end] = data[d_start:d_end]

                bytes_processed += readlength
                self.prefix_bytes_processed += readlength

                if self.prefix_bytes_processed == 4:
                    self.message_length = int.from_bytes(self.prefix_buffer, sys.byteorder)
                    self.message_length = socket.ntohl(self.message_length)
                    self.message_bytes_processed = 0
                    self.message_buffer = bytearray(self.message_length)
                    self.gathering_msg = True

            bytes_remaining = data_len - bytes_processed
            message_bytes_remaining = self.message_length - self.message_bytes_processed
            readlength = min(bytes_remaining, message_bytes_remaining)

            m_start = self.message_bytes_processed
            m_end = m_start + readlength
            d_start = bytes_processed
            d_end = d_start + readlength

            if d_end > data_len:
                raise Exception(f"Message framing error, processed {d_end} bytes for data length {data_len}")

            if m_end > self.message_length:
                raise Exception(f"Message framing error, processed {m_end} bytes for message length {self.message_length}")

            self.message_buffer[m_start:m_end] = data[d_start:d_end]

            bytes_processed += readlength
            self.message_bytes_processed += readlength

            if self.message_bytes_processed == self.message_length:
                self.prefix_bytes_processed = 0
                self.gathering_msg = False
                self.callback(self.message_buffer)

class FrameWriter:

    @staticmethod
    def encapsulate(data):
        length = len(data)
        length = socket.htonl(length)
        length_prefix = length.to_bytes(4, sys.byteorder)
        return length_prefix + data

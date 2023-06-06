import asyncio
import queue
import threading
from enum import Enum

from ..message import Message
from ...imodule import IModule
from .framing import FrameReader, FrameWriter
from .inet_client import INetClient


class NetAsyncClientPayloadType(Enum):
    # Connection - NetAsyncClientPayloadType.Data doubles as Connection since it's more reliable in the case of domain reloads.
    # Assumes that the server always sends some (*any*) data to the client on connection.

    ConnectionFailed = 0
    Disconnected = 1
    Data = 2


class NetAsyncClientPayload:

    def __init__(self, payload_type, data=None):
        self.type = payload_type
        self.data = data


class NetAsyncClient:

    def __init__(self, host, port, stop_event):
        self.host = host
        self.port = port
        self.reader = None
        self.writer = None
        self.messages_read = queue.Queue()
        self.messages_send = queue.Queue()
        self.frame_reader = FrameReader(self.frame_reader_callback)
        self.stop_event = stop_event
        self.stopped = False

    async def connect(self):
        try:
            self.reader, self.writer = await asyncio.open_connection(self.host, self.port)
        except ConnectionError:
            self.messages_read.put(NetAsyncClientPayload(NetAsyncClientPayloadType.ConnectionFailed))
            return

        await asyncio.gather(self.read_loop(), self.send_loop())

    async def read_loop(self):
        while not self.stop_event.is_set():
            try:
                data = await self.reader.read(256)
                if len(data) == 0:
                    # The server has gracefully closed this connection
                    self.messages_read.put(NetAsyncClientPayload(NetAsyncClientPayloadType.Disconnected))
                    break
            except ConnectionError:
                self.messages_read.put(NetAsyncClientPayload(NetAsyncClientPayloadType.Disconnected))
                break
            self.frame_reader.process(data)
            await asyncio.sleep(0)

        if not self.stopped:
            self.writer.close()
            try:
                await self.writer.wait_closed()
            except ConnectionResetError:
                pass
            self.stopped = True

    async def send_loop(self):
        while not self.stop_event.is_set():
            try:
                data = self.messages_send.get_nowait()
                framed = FrameWriter.encapsulate(data)
                try:
                    self.writer.write(framed)
                    await self.writer.drain()
                except ConnectionError:
                    self.messages_read.put(NetAsyncClientPayload(NetAsyncClientPayloadType.Disconnected))
                    break
            except queue.Empty:
                pass
            await asyncio.sleep(0)

        if not self.stopped:
            self.writer.close()
            self.stopped = True

    def frame_reader_callback(self, data):
        self.messages_read.put(NetAsyncClientPayload(NetAsyncClientPayloadType.Data, data))


class NetClient(INetClient, IModule):

    localhost = '127.0.0.1'

    def __init__(self):
        self.port = None
        self.connected = False
        self.client = None
        self.thread = None
        self.thread_stop_event = None
        self.connected_callbacks = []
        self.connection_failed_callbacks = []
        self.disconnected_callbacks = []
        self.callbacks = []

        self.logger = None
        self.log_connection = False
        self.log_sent = False
        self.log_received = False

    def start(self, port):
        self.port = port
        self.connected = False
        self.thread_stop_event = threading.Event()
        self.client = NetAsyncClient(NetClient.localhost, self.port, self.thread_stop_event)
        self.thread = threading.Thread(target=self.thread_func, daemon=True)
        self.thread.start()

    def stop(self):
        self.thread_stop_event.set()
        self.connected = False

    def update(self):
        for payload in self:
            if not self.connected and (payload.type == NetAsyncClientPayloadType.Data):
                self.connected = True
                self.debug_log(f"Connected successfully to server on port {self.port}", self.log_connection)
                for cb in self.connected_callbacks:
                    cb()

            if payload.type == NetAsyncClientPayloadType.Data and len(payload.data) > 1:
                msg_type = payload.data[0]
                msg_data = payload.data[1:]
                decoded = msg_data.decode()
                if self.log_received:
                    self.debug_log(f"Received message type {msg_type}: {decoded!r}")

                for cb in self.callbacks:
                    cb(Message(msg_type, msg_data))
            elif payload.type == NetAsyncClientPayloadType.ConnectionFailed:
                self.debug_log(f"Connection failed to server on port {self.port}", self.log_connection)
                for cb in self.connection_failed_callbacks:
                    cb()
            elif payload.type == NetAsyncClientPayloadType.Disconnected and self.connected:
                self.debug_log(f"Disconnected from server on port {self.port}", self.log_connection)
                for cb in self.disconnected_callbacks:
                    cb()

    def add_connected_callback(self, cb):
        if cb not in self.connected_callbacks:
            self.connected_callbacks.append(cb)

    def remove_connected_callback(self, cb):
        self.connected_callbacks.remove(cb)

    def add_connection_failed_callback(self, cb):
        if cb not in self.connection_failed_callbacks:
            self.connection_failed_callbacks.append(cb)

    def remove_connection_failed_callback(self, cb):
        self.connection_failed_callbacks.remove(cb)

    def add_disconnected_callback(self, cb):
        if cb not in self.disconnected_callbacks:
            self.disconnected_callbacks.append(cb)

    def remove_disconnected_callback(self, cb):
        self.disconnected_callbacks.remove(cb)

    def add_callback(self, cb):
        if cb not in self.callbacks:
            self.callbacks.append(cb)

    def remove_callback(self, cb):
        self.callbacks.remove(cb)

    def send(self, message):
        if self.log_sent:
            decoded = message.data.decode()
            self.debug_log(f"Sent message type {message.type}: {decoded!r}", self.log_sent)
        data = bytes([message.type]) + message.data
        self.client.messages_send.put(data)

    def thread_func(self):
        asyncio.run(self.client.connect())

    def __iter__(self):
        return self

    def __next__(self):
        try:
            payload = self.client.messages_read.get_nowait()
            return payload
        except queue.Empty:
            raise StopIteration

    def debug_log(self, str, condition=True):
        if self.logger != None and condition:
            self.logger.log(f"Client: {str}")

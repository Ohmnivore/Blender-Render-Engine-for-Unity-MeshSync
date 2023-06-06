import json
from .message import Message, IMessage, IMessageHandle


class JSONMessage:

    @staticmethod
    def to_message(imessage):
        message_type = type(imessage).get_type()

        serialized = imessage.serialize()
        text = json.dumps(serialized, default = lambda o: o.__dict__)
        data = text.encode()
        return Message(message_type, data)

    @staticmethod
    def try_parse(message, cls):
        message_type = cls.get_type()

        if message_type == message.type:
            text = message.data.decode()
            data = json.loads(text)

            parsed = cls()
            for k, v in data.items():
                setattr(parsed, k, v)
            parsed.post_deserialize()

            return parsed

        return None


class JSONMessageHandle(IMessageHandle):

    def __init__(self, message):
        self.message = message

    def try_parse(self, cls):
        return JSONMessage.try_parse(self.message, cls)

from ..comms.message import IMessage
from ..messages import ServerMessageType, ClientMessageType


class ViewUpdated(IMessage):

    @staticmethod
    def get_type():
        return ClientMessageType.ViewUpdated

    def __init__(self):
        self.view = None
        self.str_id = None

    def initialize(self, view, str_id):
        self.view = view
        self.str_id = str_id
        return self

    def serialize(self):
        data = {}
        data['ID'] = self.str_id
        data['Width'] = self.view.width
        data['Height'] = self.view.height
        data['IsPerspective'] = self.view.is_perspective
        data['ViewMatrix'] = [j for sub in self.view.view_matrix for j in sub]
        data['WindowMatrix'] = [j for sub in self.view.window_matrix for j in sub]
        return data

    def post_deserialize(self):
        pass


class ViewDestroyed(IMessage):

    @staticmethod
    def get_type():
        return ClientMessageType.ViewDestroyed

    def __init__(self):
        self.ID = None

    def initialize(self, id):
        self.ID = id
        return self

    def serialize(self):
        data = self.__dict__
        return data

    def post_deserialize(self):
        pass


class ObjectVisibility(IMessage):

    @staticmethod
    def get_type():
        return ClientMessageType.ObjectVisibility

    def __init__(self):
        self.Name = None
        self.Visible = None
        self.Obsolete = None

    def initialize(self, name, visible, obsolete):
        self.Name = name
        self.Visible = visible
        self.Obsolete = obsolete
        return self

    def serialize(self):
        data = self.__dict__
        return data

    def post_deserialize(self):
        pass

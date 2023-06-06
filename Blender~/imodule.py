from abc import ABC, abstractmethod

class IModule(ABC):

    @abstractmethod
    def start(self):
        pass

    @abstractmethod
    def stop(self):
        pass

    @abstractmethod
    def update(self):
        pass

from abc import ABC, abstractmethod


(ABC)
class ILogger:

    @abstractmethod
    def log(self, str):
        pass


class PrintLogger(ILogger):

    def __init__(self):
        pass

    def log(self, str):
        print(str)

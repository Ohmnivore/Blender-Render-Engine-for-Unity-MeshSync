import bpy
import bgl
from gpu.types import GPUOffScreen

import SpoutGL

class ViewReceiver():

    def __init__(self):
        self.name = None
        self.name_depth = None
        self.width = 0
        self.height = 0
        self.width_depth = 0
        self.height_depth = 0
        self.texture = None
        self.texture_depth = None
        self.spoutReceiver = None
        self.spoutReceiverDepth = None
        self.region = None

        self.logger = None
        self.log = False
        self.log_frame = False

    def start(self, name, name_depth):
        self.name = name
        self.name_depth = name_depth
        self.spoutReceiver = SpoutGL.SpoutReceiver()
        self.spoutReceiver.setReceiverName(self.name)
        self.spoutReceiverDepth = SpoutGL.SpoutReceiver()
        self.spoutReceiverDepth.setReceiverName(self.name_depth)
        self.debug_log(f"Started receiver {self.name} + {self.name_depth}", self.log)

    def stop(self):
        self.spoutReceiver.releaseReceiver()
        self.spoutReceiverDepth.releaseReceiver()
        self.debug_log(f"Stopped receiver {self.name} + {self.name_depth}", self.log)

    def draw(self, view_shader):
        self.region = bpy.context.region

        if (self.texture is not None and
            self.texture_depth is not None and
            self.width == self.region.width and
            self.height == self.region.height and
            self.width_depth == self.region.width and
            self.height_depth == self.region.height):
            view_shader.draw_texture_2d(self.texture.texture_color, self.texture_depth.texture_color, (0, 0), self.width, self.height)

    def update(self):
        if self.spoutReceiver.isUpdated() or self.texture is None:
            self.width = self.spoutReceiver.getSenderWidth()
            self.height = self.spoutReceiver.getSenderHeight()
            self.debug_log(f"Set receiver {self.name} size {self.width}x{self.height}", self.log)

            self.texture = GPUOffScreen(self.width, self.height, format='RGBA8')

        if self.spoutReceiver.isFrameNew():
            self.texture.bind()
            self.receive_color()
            self.texture.unbind()
            self.debug_log(f"Received new {self.name}", self.log_frame)

            if self.region is not None:
                self.region.tag_redraw()

        if self.spoutReceiverDepth.isUpdated() or self.texture_depth is None:
            self.width_depth = self.spoutReceiverDepth.getSenderWidth()
            self.height_depth = self.spoutReceiverDepth.getSenderHeight()
            self.debug_log(f"Set receiver {self.name_depth} size {self.width_depth}x{self.height_depth}", self.log)

            self.texture_depth = GPUOffScreen(self.width_depth, self.height_depth, format='RGBA32F')

        if self.spoutReceiverDepth.isFrameNew():
            self.texture_depth.bind()
            self.receive_depth()
            self.texture_depth.unbind()
            self.debug_log(f"Received new {self.name_depth}", self.log_frame)

            if self.region is not None:
                self.region.tag_redraw()

    def receive_color(self):
        self.spoutReceiver.receiveTexture(self.texture.color_texture, bgl.GL_TEXTURE_2D, True, 0)

    def receive_depth(self):
        self.spoutReceiverDepth.receiveTexture(self.texture_depth.color_texture, bgl.GL_TEXTURE_2D, True, 0)

    def debug_log(self, str, condition=True):
        if self.logger != None and condition:
            self.logger.log(f"ViewLink Receive: {str}")

import bpy

from .. import globals
from ..imodule import IModule
from .messages import ViewUpdated, ViewDestroyed
from .view import View
from .view_receiver import ViewReceiver


class ViewManager(IModule):

    def __init__(self):
        self.views = {}
        self.view_receivers = {}

        self.logger = None
        self.log_send = False
        self.log_send_frame = False
        self.log_receive = False
        self.log_receive_frame = False

    def start(self):
        globals.bridge.add_callback(self.handle_message)

    def stop(self):
        globals.bridge.remove_callback(self.handle_message)

    def update(self):
        self.find_deleted()
        self.update_receivers()

    def handle_message(self, msg):
        pass

    def update_view(self, region, region_data, view_shader):
        str_id = str(region_data.as_pointer())
        view = None
        view_receiver = None

        if str_id not in self.views:
            self.debug_log(f"Created view {str_id}", self.log_send)
            view = View()
            self.views[str_id] = view

            view_receiver = ViewReceiver()
            self.update_logging(view_receiver)
            self.view_receivers[str_id] = view_receiver
            view_receiver.start(f"MeshSync Render Engine {str_id}", f"MeshSync Render Engine Depth {str_id}")
        else:
            view = self.views[str_id]
            view_receiver = self.view_receivers[str_id]

        view.update_from_region(region, region_data)
        self.debug_log(f"Updated view {str_id}", self.log_send_frame)

        globals.bridge.send(ViewUpdated().initialize(view, str_id))

        self.update_logging(view_receiver)
        view_receiver.update()
        view_receiver.draw(view_shader)

    def destroy_view(self, str_id):
        self.debug_log(f"Destroyed view {str_id}", self.log_send)
        globals.bridge.send(ViewDestroyed().initialize(str_id))

        view_receiver = self.view_receivers[str_id]
        view_receiver.stop()

        del self.views[str_id]
        del self.view_receivers[str_id]

    def update_receivers(self):
        for receiver in self.view_receivers.values():
            receiver.update()

    def find_deleted(self):
        active_ids = set()

        for window in bpy.context.window_manager.windows:
            for area in window.screen.areas:
                for region in area.regions:
                    region_data = region.data
                    if region_data is not None:
                        str_id = str(region_data.as_pointer())
                        active_ids.add(str_id)

        to_remove = []
        for str_id in self.views:
            if str_id not in active_ids:
                to_remove.append(str_id)

        for str_id in to_remove:
            self.destroy_view(str_id)

    def update_logging(self, view_receiver):
        view_receiver.logger = self.logger
        view_receiver.log = self.log_receive
        view_receiver.log_frame = self.log_receive_frame

    def debug_log(self, str, condition=True):
        if self.logger != None and condition:
            self.logger.log(f"ViewLink Send: {str}")

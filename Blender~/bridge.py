from .imodule import IModule
from .comms.net.net_client import NetClient
from .view_link.view_manager import ViewManager
from .comms.json_message import JSONMessage, JSONMessageHandle
from .unity_render_engine.engine import UnityRenderEngine
from .messages import DomainReload
from .preferences import MeshSyncRenderEnginePreferences
from .log import PrintLogger

import bpy
import time


class Bridge(IModule):

    def __init__(self):
        self.started = False
        self.connected = False
        self.callbacks = []
        self.client = NetClient()
        self.view_manager = ViewManager()
        self.reconnecting = False

        self.logger = PrintLogger()
        self.client.logger = self.logger
        self.view_manager.logger = self.logger

    def get_preferences(self):
        preferences = bpy.context.preferences
        return preferences.addons[MeshSyncRenderEnginePreferences.bl_idname].preferences

    def update_preferences(self):
        preferences = self.get_preferences()
        self.client.log_connection = preferences.client_log_connection
        self.client.log_sent = preferences.client_log_sent
        self.client.log_received = preferences.client_log_received
        self.view_manager.log_send = preferences.view_link_send_log
        self.view_manager.log_send_frame = preferences.view_link_send_log_frame
        self.view_manager.log_receive = preferences.view_link_receive_log
        self.view_manager.log_receive_frame = preferences.view_link_receive_log_frame

    def start(self):
        scene = bpy.context.scene

        self.client.add_callback(self.handle_message)
        self.client.add_connected_callback(self.handle_connected)
        self.client.add_connection_failed_callback(self.handle_connection_failed)
        self.client.add_disconnected_callback(self.handle_disconnected)
        self.client.start(scene.msre_server_port)
        self.started = True
        self.connected = False

    def stop(self):
        if self.started:
            self.client.remove_callback(self.handle_message)
            self.client.remove_connected_callback(self.handle_connected)
            self.client.remove_connection_failed_callback(self.handle_connection_failed)
            self.client.remove_disconnected_callback(self.handle_disconnected)
            if self.connected:
                self.client.stop()
                self.view_manager.stop()
            self.started = False
        self.connected = False

    def update(self):
        self.update_preferences()
        self.client.update()
        if self.connected:
            self.view_manager.update()

    def send(self, message):
        self.client.send(JSONMessage.to_message(message))

    def add_connected_callback(self, cb):
        self.client.add_connected_callback(cb)

    def remove_connected_callback(self, cb):
        self.client.remove_connected_callback(cb)

    def add_disconnected_callback(self, cb):
        self.client.add_disconnected_callback(cb)

    def remove_disconnected_callback(self, cb):
        self.client.remove_disconnected_callback(cb)

    def add_callback(self, cb):
        if cb not in self.callbacks:
            self.callbacks.append(cb)

    def remove_callback(self, cb):
        self.callbacks.remove(cb)

    def handle_connected(self):
        if not self.reconnecting:
            self.update_preferences()
            UnityRenderEngine.disable_color_management()
            self.view_manager.start()
            self.connected = True
            bpy.ops.msre.connected()
        self.reconnecting = False

    def handle_message(self, message):
        for cb in self.callbacks:
            json_message = JSONMessageHandle(message)
            cb(json_message)

            if (domain_reload_msg := json_message.try_parse(DomainReload)) is not None and not self.reconnecting:
                scene = bpy.context.scene

                self.client.stop()

                # Give time for the socket to properly close before starting the re-connect loop
                time.sleep(0.5)

                self.client.start(scene.msre_server_port)
                self.reconnecting = True

    def handle_connection_failed(self):
        if self.reconnecting:
            scene = bpy.context.scene
            self.client.start(scene.msre_server_port)
        else:
            bpy.ops.msre.connection_failed()

    def handle_disconnected(self):
        if not self.reconnecting:
            bpy.ops.msre.stop()

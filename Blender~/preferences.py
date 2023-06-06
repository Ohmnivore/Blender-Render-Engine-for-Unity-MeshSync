import bpy
from bpy.types import AddonPreferences
from bpy.props import BoolProperty

from . import globals
from .utils.pip_manager_ui import DependenciesInstaller


class MeshSyncRenderEnginePreferences(AddonPreferences):
    bl_idname = "MeshSyncRenderEngine"

    health_monitor_expanded: BoolProperty(
        name="Health Monitor Expanded",
        default=False,
    )

    client_log_connection: BoolProperty(
        name="Log Connection",
        default=False,
    )
    client_log_sent: BoolProperty(
        name="Log Sent",
        default=False,
    )

    client_log_received: BoolProperty(
        name="Log Received",
        default=False,
    )

    view_link_send_log: BoolProperty(
        name="Log",
        default=False,
    )
    view_link_send_log_frame: BoolProperty(
        name="Log Frame",
        default=False,
    )

    view_link_receive_log: BoolProperty(
        name="Log",
        default=False,
    )
    view_link_receive_log_frame: BoolProperty(
        name="Log Frame",
        default=False,
    )

    def draw(self, context):
        layout = self.layout

        if globals.pip_manager.has_all_packages:
            layout.label(text="All dependencies are installed.")
        else:
            box = layout.box()
            for package in globals.pip_manager.packages:
                box.label(text=f"Missing dependency: {package.fully_qualified_name}")

        layout.operator(DependenciesInstaller.bl_idname, icon="PLUGIN")

        layout.separator()

        box = layout.box()
        row = box.row()
        row.prop(self, "health_monitor_expanded", text="Health Monitor",
            icon="TRIA_DOWN" if self.health_monitor_expanded else "TRIA_RIGHT",
            emboss=False
        )

        if self.health_monitor_expanded:
            box.label(text="Logs are printed to the Blender System Console.", icon="INFO")

            b = box.box()
            b.use_property_split = True
            b.label(text="Client:")
            b.prop(self, "client_log_connection")
            b.prop(self, "client_log_sent")
            b.prop(self, "client_log_received")

            b = box.box()
            b.use_property_split = True
            b.label(text="ViewLink Send:")
            b.prop(self, "view_link_send_log")
            b.prop(self, "view_link_send_log_frame")

            b = box.box()
            b.use_property_split = True
            b.label(text="ViewLink Receive:")
            b.prop(self, "view_link_receive_log")
            b.prop(self, "view_link_receive_log_frame")


def register():
    bpy.utils.register_class(MeshSyncRenderEnginePreferences)

def unregister():
    bpy.utils.unregister_class(MeshSyncRenderEnginePreferences)

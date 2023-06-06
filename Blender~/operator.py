import bpy
from bpy.app.handlers import persistent
from . import globals
from .bridge import Bridge


globals.bridge = Bridge()


class MSRE_PT_Main(bpy.types.Panel):
    bl_label = "MeshSync Render Engine"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Tool"

    def draw(self, context):
        scene = bpy.context.scene
        layout = self.layout
        layout.use_property_split = True
        layout.use_property_decorate = False

        row = layout.row()
        row.prop(scene, "msre_auto_connect")
        row.enabled = scene.msre_status == 'DISCONNECTED'

        row = layout.row()
        row.prop(scene, "msre_server_port")
        row.enabled = scene.msre_status == 'DISCONNECTED'

        layout.separator()

        row = layout.row()
        row.enabled = not scene.msre_auto_connect
        if scene.msre_status == 'DISCONNECTED':
            row.operator("msre.start", text="Start", icon="PLAY")
        elif scene.msre_status == 'CONNECTING':
            row.label(text="Connecting...", icon="TIME")
        elif scene.msre_status == 'CONNECTED':
            row.operator("msre.stop", text="Stop", icon="PAUSE")


class MSREStart(bpy.types.Operator):
    """MeshSync Render Engine Start"""
    bl_idname = "msre.start"
    bl_label = "Start"
    bl_options = {'REGISTER'}
    timer = None
    registered = False

    def __init__(self):
        pass

    def __del__(self):
        MSREStart.timer = None
        globals.bridge.stop()

    # When a file is loaded, the modal registration is reset:
    @persistent
    def load_handler(dummy):
        MSREStart.registered = False

    def modal(self, context, event):
        if event.type == 'TIMER':
            globals.bridge.update()
        return {'PASS_THROUGH'}

    def invoke(self, context, event):
        scene = bpy.context.scene
        if not MSREStart.timer:
            scene.msre_status = 'CONNECTING'

            MSREStart.timer = context.window_manager.event_timer_add(1.0 / 100.0, window=context.window)

            if not MSREStart.registered:
                context.window_manager.modal_handler_add(self)
                MSREStart.registered = True

            globals.bridge.start()

            return {'RUNNING_MODAL'}
        else:
            return {'FINISHED'}

    def execute(self, context):
        return self.invoke(context, None)


class MSREConnected(bpy.types.Operator):
    """MeshSync Render Engine Connected"""
    bl_idname = "msre.connected"
    bl_label = "Connected"
    bl_options = {'REGISTER', 'INTERNAL'}

    def invoke(self, context, event):
        scene = bpy.context.scene
        scene.msre_status = 'CONNECTED'
        return {'FINISHED'}

    def execute(self, context):
        return self.invoke(context, None)


class MSREConnectionFailed(bpy.types.Operator):
    """MeshSync Render Engine Connection Failed"""
    bl_idname = "msre.connection_failed"
    bl_label = "Connection failed"
    bl_options = {'REGISTER', 'INTERNAL'}

    def invoke(self, context, event):
        scene = bpy.context.scene
        if MSREStart.timer:
            def draw_message(self, context):
                self.layout.label(text=f"Failed to connect to a Unity instance")
            bpy.context.window_manager.popup_menu(draw_message, title="Error", icon='ERROR')
            self.report({'WARNING'}, f"Failed to connect to a Unity instance")

            scene.msre_status = 'DISCONNECTED'
            context.window_manager.event_timer_remove(MSREStart.timer)
            MSREStart.timer = None
            return {'FINISHED'}

    def execute(self, context):
        return self.invoke(context, None)


class MSREStop(bpy.types.Operator):
    """MeshSync Render Engine Stop"""
    bl_idname = "msre.stop"
    bl_label = "Stop"
    bl_options = {'REGISTER'}

    def invoke(self, context, event):
        scene = bpy.context.scene
        if MSREStart.timer:
            scene.msre_status = 'DISCONNECTED'
            context.window_manager.event_timer_remove(MSREStart.timer)
            MSREStart.timer = None
            globals.bridge.stop()
            return {'FINISHED'}

    def execute(self, context):
        return self.invoke(context, None)


def status_update(self, context):
    if context.area is not None:
        for region in context.area.regions:
            if region.type == "UI":
                region.tag_redraw()

class ReentrantGuard(object):
    def __init__(self):
        self.value = False
     
    def __enter__(self):
        self.value = True
        return self.value
 
    def __exit__(self, *args):
        self.value = False

reentrant_guard = ReentrantGuard()

@persistent
def auto_connect(scene):
    if not scene.msre_auto_connect or reentrant_guard.value:
        return

    if (meshsync_auto_sync := scene.get('meshsync_auto_sync')) is not None:
        if meshsync_auto_sync and scene.msre_status == 'DISCONNECTED':
            with reentrant_guard:
                bpy.ops.msre.start()
        elif not meshsync_auto_sync and scene.msre_status == 'CONNECTED':
            with reentrant_guard:
                bpy.ops.msre.stop()


def register():
    bpy.app.handlers.load_post.append(MSREStart.load_handler)
    bpy.app.handlers.depsgraph_update_post.append(auto_connect)

    bpy.utils.register_class(MSRE_PT_Main)
    bpy.utils.register_class(MSREStart)
    bpy.utils.register_class(MSREConnected)
    bpy.utils.register_class(MSREConnectionFailed)
    bpy.utils.register_class(MSREStop)

    bpy.types.Scene.msre_status = bpy.props.EnumProperty(
        items=(('DISCONNECTED', 'Disconnected', ''), ('CONNECTING', 'Connecting', ''), ('CONNECTED', 'Connected', '')),
        name="MeshSync Render Engine Status",
        default='DISCONNECTED',
        update=status_update)

    bpy.types.Scene.msre_auto_connect = bpy.props.BoolProperty(
        name="Auto-Connect",
        description="Automatically connects/disconnects MeshSync Render Engine when MeshSync connects/disconnects",
        default=True,
    )

    bpy.types.Scene.msre_server_port = bpy.props.IntProperty(
        name="Server Port",
        default=51451,
        min=0,
        max=65535)

def unregister():
    bpy.utils.unregister_class(MSRE_PT_Main)
    bpy.utils.unregister_class(MSREStart)
    bpy.utils.unregister_class(MSREConnected)
    bpy.utils.unregister_class(MSREConnectionFailed)
    bpy.utils.unregister_class(MSREStop)

import bpy
import array
import gpu

from .. import globals
from . import material_ui
from .visibility_cache import VisibilityCache
from ..view_link.view_shader import ViewShader
from ..view_link.messages import ObjectVisibility


class UnityRenderEngine(bpy.types.RenderEngine):
    bl_idname = "UNITY"
    bl_label = "Unity"
    bl_use_preview = True
    shader = None

    def __init__(self):
        self.first_update = True
        self.visibility_cache = VisibilityCache()

    def __del__(self):
        pass

    def render(self, depsgraph):
        pass

    def view_update(self, context, depsgraph):
        if not globals.bridge.connected:
            return

        view3d = context.space_data

        if self.first_update:
            self.first_update = False

            for obj in depsgraph.scene.objects:
                self.update_object_visibility(obj, depsgraph, view3d)
            self.update_visibility_cache()

        else:
            has_updated_visibility = False
            scene_id = depsgraph.scene.evaluated_get(depsgraph)

            for update in depsgraph.updates:
                if update.id == scene_id:
                    for obj in depsgraph.scene.objects:
                        has_updated_visibility = True
                        self.update_object_visibility(obj, depsgraph, view3d)

            if has_updated_visibility:
                self.update_visibility_cache()

    def view_draw(self, context, depsgraph):
        if globals.bridge != None and not globals.bridge.connected:
            return

        if UnityRenderEngine.shader is None:
            UnityRenderEngine.shader = ViewShader()
            UnityRenderEngine.shader.load()

        scene = depsgraph.scene
        region = context.region
        region_data = context.region_data

        gpu.state.blend_set('ALPHA_PREMULT')

        globals.bridge.view_manager.update_view(region, region_data, UnityRenderEngine.shader)

        gpu.state.blend_set('NONE')

    def update_object_visibility(self, obj, depsgraph, view3d):
        obj_evaluated = obj.evaluated_get(depsgraph)
        has_changed, entry = self.visibility_cache.update(obj_evaluated, view3d)
        if has_changed:
            globals.bridge.send(ObjectVisibility().initialize(obj_evaluated.name, entry.visible, False))

    def update_visibility_cache(self):
        obsolete_objects = self.visibility_cache.get_obsolete_objects()
        if obsolete_objects != None:
            for obj_name in obsolete_objects:
                globals.bridge.send(ObjectVisibility().initialize(obj_name, False, True))

    @staticmethod
    def disable_color_management():
        bpy.context.scene.display_settings.display_device = 'None'


def get_panels():
    exclude_panels = {
        'VIEWLAYER_PT_filter',
        'VIEWLAYER_PT_layer_passes',
    }

    panels = []
    for panel in bpy.types.Panel.__subclasses__():
        if hasattr(panel, 'COMPAT_ENGINES') and 'BLENDER_RENDER' in panel.COMPAT_ENGINES:
            if panel.__name__ not in exclude_panels:
                panels.append(panel)

    return panels


def register():
    bpy.utils.register_class(UnityRenderEngine)
    material_ui.register()

    for panel in get_panels():
        panel.COMPAT_ENGINES.add('UNITY')

def unregister():
    bpy.utils.unregister_class(UnityRenderEngine)
    material_ui.unregister()

    for panel in get_panels():
        if 'UNITY' in panel.COMPAT_ENGINES:
            panel.COMPAT_ENGINES.remove('UNITY')

bl_info = {
    "name": "MeshSync Render Engine",
    "blender": (3, 3, 3),
    "category": "Import-Export",
}


import bpy

from . import globals
from .utils import pip_manager_ui
from .utils.pip_manager import Package
from . import preferences


def register():
    pip_manager_ui.register()
    globals.pip_manager.needs_package(Package("SpoutGL", version="==0.0.4"))
    globals.pip_manager.startup()

    preferences.register()

    if globals.pip_manager.is_operational():
        from . import operator
        from .unity_render_engine import engine

        operator.register()
        engine.register()

def unregister():
    if globals.pip_manager.was_operational_at_startup():
        from . import preferences
        from . import operator
        from .unity_render_engine import engine

        engine.unregister()
        operator.unregister()

    preferences.unregister()

    pip_manager_ui.unregister()


if __name__ == "__main__":
    register()

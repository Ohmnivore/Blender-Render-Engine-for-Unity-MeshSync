
import bpy

from .. import globals
from .pip_manager import PipManager


class MSRE_PT_DependenciesWarningPanel(bpy.types.Panel):
    bl_idname = 'MSRE_PT_DependenciesWarningPanel'
    bl_label = "MeshSync Render Engine Dependencies"
    bl_category = "Tool"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"

    @classmethod
    def poll(self, context):
        return not globals.pip_manager.has_all_packages

    def draw(self, context):
        layout = self.layout

        layout = self.layout

        package_names = ", ".join(package.fully_qualified_name for package in globals.pip_manager.packages)

        text = ["Please install the following missing Python packages to use the MeshSync Render Engine add-on:",
                package_names,
                "They can be installed automatically by the following steps:",
                "1. Open the MeshSync Render Engine preferences: Edit > Preferences > Add-ons > MeshSync Render Engine.",
                "2. Expand the details section of the add-on.",
                "3. Click on the \"Install dependencies\" button to download and install the packages.",
                "4. Restart Blender."]

        for line in text:
            layout.label(text=line)


class MSRE_PT_DependenciesRestartWarningPanel(bpy.types.Panel):
    bl_idname = 'MSRE_PT_DependenciesRestartWarningPanel'
    bl_label = "MeshSync Render Engine Dependencies (Restart)"
    bl_category = "Tool"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"

    @classmethod
    def poll(self, context):
        return globals.pip_manager.has_all_packages and globals.pip_manager.has_installed_packages

    def draw(self, context):
        layout = self.layout

        layout = self.layout
        text = []

        text = ["Python package dependencies were just installed for the MeshSync Render Engine add-on.",
                "Please restart Blender so that they can be loaded."]

        for line in text:
            layout.label(text=line)


class DependenciesInstaller(bpy.types.Operator):
    bl_idname = "ui.dependencies_installer"
    bl_label = "Install dependencies"
    bl_description = ("Installs the Python package dependencies for MeshSync Render Engine from PyPI through pip")
    bl_options = {"REGISTER", "INTERNAL"}

    @classmethod
    def poll(self, context):
        return not globals.pip_manager.has_all_packages

    def execute(self, context):
        manager = globals.pip_manager

        manager.has_all_packages = False
        manager.has_installed_packages = False

        for package in manager.packages:
            if not manager.is_installed(package):
                self.report({'INFO'}, f"Installing MeshSync Render Engine dependency: {package.fully_qualified_name}")

                if not manager.install(package):
                    self.report({'ERROR'}, f"Failed to install MeshSync Render Engine dependency: {package.fully_qualified_name}")
                    return {"CANCELLED"}
                else:
                    manager.has_installed_packages = True

        if manager.has_installed_packages:
            self.report({'WARNING'}, f"Please restart Blender to load newly installed dependencies")

        manager.has_all_packages = True

        return {'FINISHED'}


def register():
    globals.pip_manager = PipManager()
    globals.pip_manager.reset()

    bpy.utils.register_class(MSRE_PT_DependenciesWarningPanel)
    bpy.utils.register_class(MSRE_PT_DependenciesRestartWarningPanel)
    bpy.utils.register_class(DependenciesInstaller)

def unregister():
    bpy.utils.unregister_class(MSRE_PT_DependenciesWarningPanel)
    bpy.utils.unregister_class(MSRE_PT_DependenciesRestartWarningPanel)
    bpy.utils.unregister_class(DependenciesInstaller)

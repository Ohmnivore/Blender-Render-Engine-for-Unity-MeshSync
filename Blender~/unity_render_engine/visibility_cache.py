import bpy

class VisibilityCache:

    class Entry:

        def __init__(self, name, visible, version):
            self.name = name
            self.update(visible, version)

        def update(self, visible, version):
            self.visible = visible
            self.version = version

    def __init__(self):
        self.entries = {}
        self.version = 0
        self.changed_flag = False

    def update(self, obj, space_view_3d):
        has_changed = False
        self.set_changed()
        obj_name = obj.name
        obj_visible = self.is_visible_in_view(obj, space_view_3d)

        # Object names are guaranteed to be unique in Blender
        if obj_name in self.entries:
            entry = self.entries[obj_name]
            has_changed = entry.visible != obj_visible
            entry.update(obj_visible, self.version)
        else:
            has_changed = True
            entry = self.Entry(obj_name, obj_visible, self.version)
            self.entries[obj_name] = entry

        return (has_changed, entry)

    # Unused, keeping around just in case
    def get_path_for_meshsync(self, obj):
        current_obj = obj
        path = obj.name

        while current_obj.parent != None:
            current_obj = current_obj.parent
            path = current_obj.name + "/" + path

        return path

    # From https://projects.blender.org/blender/blender/issues/95197
    def is_visible_in_view(self, obj, space_view_3d):
        if type(space_view_3d) != bpy.types.SpaceView3D:
            raise TypeError(f'The view is incorrect, expected a SpaceView3D type, not {type(space_view_3d)}')

        if (hasattr(obj, 'visible_in_viewport_get')):
            return obj.visible_in_viewport_get(space_view_3d)
        else:
            return obj.local_view_get(space_view_3d)

    def get_obsolete_objects(self):
        if self.changed_flag:
            self.changed_flag = False
            obsolete_objects = []

            for obj_name in self.entries:
                entry = self.entries[obj_name]
                if entry.version < self.version:
                    obsolete_objects.append(obj_name)

            for obj_name in obsolete_objects:
                del self.entries[obj_name]

            return obsolete_objects

        return None

    def set_changed(self):
        if not self.changed_flag:
            self.version = self.version + 1
        self.changed_flag = True

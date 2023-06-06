import bpy
from bpy.types import Panel
from bpy_extras.node_utils import find_node_input


def traverse_reroutes(node):
    if node is None or node.bl_idname != "NodeReroute":
        return node

    for node_socket in node.inputs:
        if node_socket.is_linked:
            input_node = node_socket.links[0].from_node
            return traverse_reroutes(input_node)

    return None

def get_input_node(node_socket):
    if node_socket.is_linked:
        input_node = node_socket.links[0].from_node
        return traverse_reroutes(input_node)
    return None


class MaterialButtonsPanel:
    bl_space_type = 'PROPERTIES'
    bl_region_type = 'WINDOW'
    bl_context = "material"
    # COMPAT_ENGINES must be defined in each subclass, external engines can add themselves here

    @classmethod
    def poll(cls, context):
        mat = context.material
        return mat and (context.engine in cls.COMPAT_ENGINES) and not mat.grease_pencil


def panel_node_draw(layout, ntree, _output_type, input_name):
    node = ntree.get_output_node('ALL')
    if node == None:
        node = ntree.get_output_node('EEVEE')
    if node == None:
        node = ntree.get_output_node('CYCLES')

    if node:
        input = find_node_input(node, input_name)
        if input:
            input_node = get_input_node(input)
            if input_node != None and input_node.bl_idname == "ShaderNodeHoldout":
                layout.label(text="Unity MeshSync doesn't support the Holdout node.", icon="ERROR")
            layout.template_node_view(ntree, node, input)
        else:
            layout.label(text="Incompatible output node")
    else:
        layout.label(text="No output node")


class UNITY_MATERIAL_PT_surface(MaterialButtonsPanel, Panel):
    bl_label = "Surface"
    bl_context = "material"
    COMPAT_ENGINES = {'UNITY'}

    def draw(self, context):
        layout = self.layout

        mat = context.material

        layout.prop(mat, "use_nodes", icon='NODETREE')
        layout.separator()

        layout.use_property_split = True

        if mat.use_nodes:
            panel_node_draw(layout, mat.node_tree, 'OUTPUT_MATERIAL', "Surface")
        else:
            layout.label(text="This material doesn't use nodes.", icon="ERROR")
            layout.label(text="Unity MeshSync only supports node-based materials.")


def register():
    bpy.utils.register_class(UNITY_MATERIAL_PT_surface)
    bpy.types.EEVEE_MATERIAL_PT_context_material.COMPAT_ENGINES.add('UNITY')

def unregister():
    bpy.utils.unregister_class(UNITY_MATERIAL_PT_surface)

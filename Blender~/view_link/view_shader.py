import bgl
import gpu
from gpu_extras.batch import batch_for_shader


class ViewShader:

    def __init__(self):
        self.shader = None

    def load(self):
        vert_out = gpu.types.GPUStageInterfaceInfo("view_interface")
        vert_out.smooth('VEC2', "texCoord_interp")

        # Same as gpu_shader_3D_image_common
        shader_info = gpu.types.GPUShaderCreateInfo()
        shader_info.vertex_in(0, 'VEC3', "pos")
        shader_info.vertex_in(1, 'VEC2', "texCoord")
        shader_info.vertex_out(vert_out)
        shader_info.fragment_out(0, 'VEC4', "fragColor")
        shader_info.push_constant('MAT4', "ModelViewProjectionMatrix")
        shader_info.sampler(0, 'FLOAT_2D', "image")
        shader_info.sampler(1, 'FLOAT_2D', "depthImage")

        # Same as gpu_shader_3D_image_vert.glsl
        shader_info.vertex_source(
            "void main()"
            "{"
            "  gl_Position = ModelViewProjectionMatrix * vec4(pos.xyz, 1.0f);"
            "  texCoord_interp = texCoord;"
            "}"
        )

        # Same as gpu_shader_image_frag.glsl
        shader_info.fragment_source(
            # From From https://stackoverflow.com/a/48138528
            "float DecodeFloatRGBA(vec4 enc)" 
            "{"
            "    uint ex = uint(enc.x * 255.0f);"
            "    uint ey = uint(enc.y * 255.0f);"
            "    uint ez = uint(enc.z * 255.0f);"
            "    uint ew = uint(enc.w * 255.0f);"
            "    uint v = (ex << 24) + (ey << 16) + (ez << 8) + ew;"
            "    return v / (256.0f * 256.0f * 256.0f * 256.0f);"
            "}"
            ""
            "void main()"
            "{"
            "  vec4 encodedDepth = texelFetch(depthImage, ivec2(gl_FragCoord.xy), 0);"
            "  float decodedDepth = DecodeFloatRGBA(encodedDepth);"
            ""
            "  gl_FragDepth = decodedDepth;"
            ""
            "  fragColor = texture(image, texCoord_interp);"
            "}"
        )

        self.shader = gpu.shader.create_from_info(shader_info)

    # Similar to gpu_extras.presets.draw_texture_2d
    def draw_texture_2d(self, texture, depth_texture, position, width, height):
        pos_coords = ((0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0))
        tex_coords = ((0, 0), (1, 0), (1, 1), (0, 1))

        batch = batch_for_shader(
            self.shader, 'TRI_FAN',
            {"pos": pos_coords, "texCoord": tex_coords},
        )

        with gpu.matrix.push_pop():
            gpu.matrix.translate(position)
            gpu.matrix.scale((width, height))

            if isinstance(texture, int):
                # Call the legacy bgl to not break the existing API
                bgl.glActiveTexture(bgl.GL_TEXTURE0)
                bgl.glBindTexture(bgl.GL_TEXTURE_2D, texture)
                self.shader.uniform_int("image", 0)
                bgl.glActiveTexture(bgl.GL_TEXTURE1)
                bgl.glBindTexture(bgl.GL_TEXTURE_2D, depth_texture)
                self.shader.uniform_int("depthImage", 1)
            else:
                self.shader.uniform_sampler("image", texture)
                self.shader.uniform_sampler("depthImage", depth_texture)

            gpu.state.depth_mask_set(True)
            gpu.state.depth_test_set('ALWAYS')
            batch.draw(self.shader)

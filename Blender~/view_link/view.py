class View():

    def __init__(self):
        self.width = None
        self.height = None
        self.is_perspective = None
        self.view_matrix = None
        self.window_matrix = None

    def update_from_region(self, region, region_data):
        self.width = region.width
        self.height = region.height
        self.is_perspective = region_data.is_perspective
        self.view_matrix = region_data.view_matrix
        self.window_matrix = region_data.window_matrix

    def debug_print(self):
        print(f"Size: {self.width} x {self.height}")
        print(f"Perspective: {self.is_perspective}")
        print("View matrix:")
        print(*self.view_matrix[0], sep = ", ")
        print(*self.view_matrix[1], sep = ", ")
        print(*self.view_matrix[2], sep = ", ")
        print(*self.view_matrix[3], sep = ", ")
        print("Window matrix:")
        print(*self.window_matrix[0], sep = ", ")
        print(*self.window_matrix[1], sep = ", ")
        print(*self.window_matrix[2], sep = ", ")
        print(*self.window_matrix[3], sep = ", ")

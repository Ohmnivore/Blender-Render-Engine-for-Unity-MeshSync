import site
import sys
import subprocess
import ensurepip
import os


class Package:

    def __init__(self, name, version):
        self.name = name
        self.version = version
        self.fully_qualified_name = name + version


class PipManager():

    def __init__(self):
        self.packages = []
        self.had_all_packages_at_startup = False
        self.has_all_packages = False
        self.has_installed_packages = False

    def reset(self):
        self.packages.clear()
        self.has_all_packages = False
        self.had_all_packages_at_startup = False
        self.has_installed_packages = False

    def verify_user_sitepackages(self):
        usersitepackagespath = site.getusersitepackages()
        if os.path.exists(usersitepackagespath) and usersitepackagespath not in sys.path:
            sys.path.append(usersitepackagespath)

    def ensure_pip(self):
        ensurepip.bootstrap()

    def is_installed(self, package):
        self.verify_user_sitepackages()
        try:
            __import__(package.name)
            return True
        except ModuleNotFoundError:
            return False

    def install(self, package):
        self.ensure_pip()
        pybin = sys.executable

        try:
            subprocess.run([pybin, '-m', 'pip', 'install', "--upgrade", package.fully_qualified_name], check=True)
            return True
        except subprocess.CalledProcessError:
            return False

    def needs_package(self, package):
        self.packages.append(package)

    def startup(self):
        self.has_all_packages = all(self.is_installed(package) for package in self.packages)
        self.had_all_packages_at_startup = self.has_all_packages

    def is_operational(self):
        return self.has_all_packages and not self.has_installed_packages

    def was_operational_at_startup(self):
        return self.had_all_packages_at_startup

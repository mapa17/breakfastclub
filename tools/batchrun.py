import subprocess
import sys

from pudb import set_trace as st

LINUX = 0
MACOS = 1
WINDOWS = 2
UNKNOWN = 3

# -nographics causes problems, not writing player.log file ...
#/usr/bin/open -W -n ../Unity/build/CurrentBuild.app --args -batchmode GameConfig.json 2332


def detectOS():
    OS = {'linux': LINUX, 'win32': WINDOWS, 'darwin': MACOS}
    if sys.platform in OS:
        return OS[sys.platform]
    else:
        return UNKNOWN

st()

MACOS_CMD = ["/usr/bin/open", "-W", "-n", "../Unity/build/CurrentBuild.app", "--args" ,"-batchmode"]

def run_instance(systemos, config_file, seed):
    if systemos == MACOS:
        try:
            subprocess.run(MACOS_CMD + [config_file, seed], check=True, shell=False)
        except subprocess.CalledProcessError as e:
            print(e.args, e.returncode, e.stderr)
            return 1
        return 0

# Very simple argument parser
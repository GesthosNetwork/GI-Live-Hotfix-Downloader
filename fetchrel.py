import sys
import os
import json
import requests
import logging
import signal
from shutil import copyfileobj
from posixpath import join as urljoin
from argparse import ArgumentParser

DEFAULT_CLIENTS = [
  "StandaloneWindows64",
  "iOS",
  "Android",
  "PS4",
  "PS5"
]

RES_BASE_PATH = "client_game_res"
RES_FILES = {
  "res_versions_external": True,
  "res_versions_medium": False,
  "res_versions_streaming": False,
  "release_res_versions_external": True,
  "release_res_versions_medium": False,
  "release_res_versions_streaming": False
}
RES_UNLISTED_FILES = [
  "base_revision",
  "script_version"
]
RES_LEGACY_FILES = [
  "VideoAssets/video_versions",
  "AudioAssets/audio_versions"
]
RES_AUDIO_DIFF_FILE = "audio_diff_versions"

SILENCE_BASE_PATH = "client_design_data"
SILENCE_FILES = [
  "AssetBundles/data_versions"
]

DATA_BASE_PATH = "client_design_data"
DATA_FILES = [
  "AssetBundles/data_versions_medium",
  "AssetBundles/data_versions"
]

DIR_MAPPINGS = {
  ".blk": "AssetBundles",
  ".pck": "AudioAssets",
  ".cuepoint": "VideoAssets",
  ".srt": "VideoAssets",
  ".usm": "VideoAssets"
}
AUDIO_DIFF_DIR = "AudioDiff"

NAME_MAPPINGS = {
  "svc_catalog": "AssetBundles"
}

def logger():
  return logging.getLogger(os.path.basename(__file__))

def sigint_handler(signal, frame):
  logger().info("Interrupted but nothing to clean up yet; exiting cleanly")
  sys.exit(130)

def fetch_file(rel_path, base_url, dst_dir):
  if os.path.exists(f"{dst_dir}/{rel_path}"):
    logger().debug(f"File {rel_path} already exists; skipping")
    return

  logger().info(f"Fetching {rel_path}...")
  try:
    response = requests.get(urljoin(base_url, rel_path), stream=True)
  except Exception as err:
    logger().error(f"Failed to fetch {rel_path}; {err=}")
    return

  if response.status_code != 200:
    logger().error(f"Failed to fetch {rel_path}; status_code={response.status_code}")
    return

  os.makedirs(os.path.dirname(f"{dst_dir}/{rel_path}"), exist_ok=True)

  try:
    signal.signal(signal.SIGINT, signal.default_int_handler)
    with open(f"{dst_dir}/{rel_path}", "wb") as file:
      copyfileobj(response.raw, file, length=16*1024*1024)
  except KeyboardInterrupt:
    logger().warning("Catched KeyboardInterrupt during download process; cleaning up partial files...")
    os.remove(f"{dst_dir}/{rel_path}")
    logger().info("Exiting cleanly")
    sys.exit(130)

  signal.signal(signal.SIGINT, sigint_handler)

def parse_res_versions(name, rel_path, base_url, dst_dir, is_base, is_audio):
  logger().info(f"Parsing {name} with {{{rel_path=}, {is_base=}}}")

  if not os.path.exists(f"{dst_dir}/{rel_path}/{name}"):
    logger().error(f"Unable to parse {rel_path}/{name}; file does not exist")
    return

  file = open(f"{dst_dir}/{rel_path}/{name}")
  while True:
    line = file.readline()
    if not line:
      break

    try:
      resource = json.loads(line.strip())

      remote_name = resource.get("remoteName")
      is_patch = resource.get("isPatch")
    except json.decoder.JSONDecodeError:
      resource = line.strip().split(' ')

      remote_name = resource[0]
      try:
        is_patch = True if resource[2] == 'P' else False
      except IndexError:
        is_patch = False

    remote_dir = DIR_MAPPINGS.get(os.path.splitext(remote_name)[1]) or NAME_MAPPINGS.get(remote_name) or ""

    if is_base == False and is_patch != True:
      continue
    if not is_audio and remote_dir in ["AudioAssets", "VideoAssets"]:
      continue

    fetch_file(urljoin(rel_path, remote_dir, remote_name), base_url, dst_dir)

def parse_data_versions(name, rel_path, base_url, dst_dir):
  logger().info(f"Parsing {name} with {{{rel_path=}}}")

  if not os.path.exists(f"{dst_dir}/{rel_path}/{name}"):
    logger().error(f"Unable to parse {rel_path}/{name}; file does not exist")
    return

  file = open(f"{dst_dir}/{rel_path}/{name}")
  while True:
    line = file.readline()
    if not line:
      break

    try:
      remote_name = json.loads(line.strip()).get("remoteName")
    except json.decoder.JSONDecodeError:
      remote_name = line.strip().split(' ')[0]

    remote_dir = DIR_MAPPINGS.get(os.path.splitext(remote_name)[1]) or NAME_MAPPINGS.get(remote_name) or ""
    fetch_file(urljoin(rel_path, remote_dir, remote_name), base_url, dst_dir)

def parse_audio_versions(name, rel_path, base_url, dst_dir, diff):
  logger().info(f"Parsing {name} with {{{rel_path=}, {diff=}}}")

  if not os.path.exists(f"{dst_dir}/{rel_path}/{name}"):
    logger().error(f"Unable to parse {rel_path}/{name}; file does not exist")
    return

  file = open(f"{dst_dir}/{rel_path}/{name}")
  while True:
    line = file.readline()
    if not line:
      break

    try:
      remote_name = json.loads(line.strip()).get("remoteName")
    except json.decoder.JSONDecodeError:
      remote_name = line.strip().split(' ')[0]

    remote_dir = f"{AUDIO_DIFF_DIR}_{diff}"
    fetch_file(urljoin(rel_path, remote_dir, remote_name), base_url, dst_dir)

def main(args):
  logger().info(f"Initialized with {args=}")

  if not args.clients:
    logger().info(f"Using default client list of {DEFAULT_CLIENTS}")
    args.clients = DEFAULT_CLIENTS

  if args.command == 'res':
    for client in args.clients:
      for name, is_audio in RES_FILES.items():
        fetch_file(f"{RES_BASE_PATH}/{args.branch}/output_{args.revision}/client/{client}/{name}", args.url, args.out)
        parse_res_versions(name, f"{RES_BASE_PATH}/{args.branch}/output_{args.revision}/client/{client}", args.url, args.out, args.base, is_audio)
      for name in (RES_LEGACY_FILES + RES_UNLISTED_FILES):
        fetch_file(f"{RES_BASE_PATH}/{args.branch}/output_{args.revision}/client/{client}/{name}", args.url, args.out)
      if args.audio:
        fetch_file(f"{RES_BASE_PATH}/{args.branch}/output_{args.revision}/client/{client}/{RES_AUDIO_DIFF_FILE}_{args.audio}", args.url, args.out)
        parse_audio_versions(f"{RES_AUDIO_DIFF_FILE}_{args.audio}", f"{RES_BASE_PATH}/{args.branch}/output_{args.revision}/client/{client}", args.url, args.out, args.audio)
  elif args.command == 'data':
    for name in DATA_FILES:
      fetch_file(f"{DATA_BASE_PATH}/{args.branch}/output_{args.revision}/client/General/{name}", args.url, args.out)
      parse_data_versions(name, f"{DATA_BASE_PATH}/{args.branch}/output_{args.revision}/client/General", args.url, args.out)
  elif args.command == 'silence':
    for name in SILENCE_FILES:
      fetch_file(f"{SILENCE_BASE_PATH}/{args.branch}/output_{args.revision}/client_silence/General/{name}", args.url, args.out)
      parse_data_versions(name, f"{SILENCE_BASE_PATH}/{args.branch}/output_{args.revision}/client_silence/General", args.url, args.out)

if __name__ == "__main__":
  parser = ArgumentParser(description="Fetches hot-patch resources for given version")
  parser.add_argument("--branch", type=str, required=True, help="Branch name (eg. 3.2_live)")
  parser.add_argument("--client", "-c", help="Client type; can be provided multiple times or left empty for default", action="append", dest='clients')
  parser.add_argument("--out", "-o", help="Output directory (defaults to ./resources)", type=str, default="./resources")
  parser.add_argument("--url", "-u", help="Base URL to download from, including protocol (defaults to https://autopatchhk.yuanshen.com)", type=str, default="https://autopatchhk.yuanshen.com")
  parser.add_argument("--verbose", "-v", help="Be more verbose", action="store_true")
  subparsers = parser.add_subparsers(help='Type of resource to fetch', required=True, dest='command')

  res_parser = subparsers.add_parser('res')
  res_parser.add_argument("revision", type=str, help="Resource reversion in format of {$revision}_{$suffix}")
  res_parser.add_argument("--base", "-b", help="Whether given revision is base (will attempt to fetch all resources)", action="store_true")
  res_parser.add_argument("--audio", "-a", help="Audio diff in format of {$current}-{$previous}")

  data_parser = subparsers.add_parser('data')
  data_parser.add_argument("revision", type=str, help="Client data reversion in format of {$revision}_{$suffix}")

  silence_parser = subparsers.add_parser('silence')
  silence_parser.add_argument("revision", type=str, help="Client silence reversion in format of {$revision}_{$suffix}")

  args = parser.parse_args()

  logger().setLevel(logging.DEBUG if args.verbose else logging.INFO)
  handler = logging.StreamHandler()
  handler.setFormatter(logging.Formatter("[%(asctime)s] <%(levelname)s> %(message)s"))
  logger().addHandler(handler)

  signal.signal(signal.SIGINT, sigint_handler)

  sys.exit(main(args))
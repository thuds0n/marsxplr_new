#!/usr/bin/env python3
"""Serve Whirld world files to the original Mars Explorer running under Wine.

The original's world hosts are gone (papawheely.com, old Dropbox links), and
its Fort Boxy download from Amazon S3 fails under Wine with "World loading
error: syntax error". This serves world files from this Mac instead. In the
game, choose Host -> Use Custom World and enter, for example:

    http://127.0.0.1:8765/FortBoxy.utw

Files are served from reference/whirlds/ (tracked) and archive/whirlds/
(local, ignored by Git), in that order.

With --relay, requests are instead logged in full (request line, headers and
the start of the response) and forwarded to the original S3 bucket, to find
out what the player actually sends.

Usage:
    tools/whirld-server.py [--port 8765]
    tools/whirld-server.py --relay [--port 8766]
Stop with Ctrl-C.
"""

import argparse
import http.client
import http.server
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
FOLDERS = [ROOT / "reference" / "whirlds", ROOT / "archive" / "whirlds"]
S3_HOST = "map.marsxplr.com.s3.amazonaws.com"


class ServeHandler(http.server.SimpleHTTPRequestHandler):
    def translate_path(self, path):
        name = pathlib.PurePosixPath(path.split("?", 1)[0]).relative_to("/")
        for folder in FOLDERS:
            candidate = (folder / name).resolve()
            if candidate.is_relative_to(folder.resolve()) and candidate.is_file():
                return str(candidate)
        return str(FOLDERS[0] / name)


class RelayHandler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        print(f"\n> {self.requestline}", flush=True)
        for key, value in self.headers.items():
            print(f"> {key}: {value}", flush=True)
        headers = {k: v for k, v in self.headers.items() if k.lower() != "host"}
        headers["Host"] = S3_HOST
        connection = http.client.HTTPConnection(S3_HOST, timeout=30)
        connection.request(self.command, self.path, headers=headers)
        response = connection.getresponse()
        body = response.read()
        print(f"< {response.status} {response.reason}, {len(body)} bytes", flush=True)
        print(f"< {body[:120]!r}", flush=True)
        self.send_response(response.status, response.reason)
        for key, value in response.getheaders():
            if key.lower() not in ("transfer-encoding", "connection"):
                self.send_header(key, value)
        self.end_headers()
        self.wfile.write(body)

    do_HEAD = do_GET


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--relay", action="store_true", help="log and forward requests to S3")
    parser.add_argument("--port", type=int, help="port (default 8765, or 8766 with --relay)")
    args = parser.parse_args()
    port = args.port or (8766 if args.relay else 8765)
    handler = RelayHandler if args.relay else ServeHandler
    server = http.server.ThreadingHTTPServer(("127.0.0.1", port), handler)
    if args.relay:
        print(f"Relaying http://127.0.0.1:{port}/<file> to http://{S3_HOST}/<file>")
    else:
        files = sorted({p.name for f in FOLDERS if f.is_dir() for p in f.iterdir() if p.is_file()})
        print(f"Serving on http://127.0.0.1:{port}/: " + ", ".join(files))
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        sys.exit(0)


if __name__ == "__main__":
    main()

# Third-party notices

The iStream release package includes the `engine/` folder: the unmodified Windows build of the UxPlay AirPlay server
from **uxplay-windows 2.0.0.1736** by leapbtw. It's distributed under its own licenses, listed below.
The engine's own license file is included as `engine/LICENSE.rtf`.

| Component | License | Source |
|---|---|---|
| UxPlay (AirPlay mirroring server) | GPL-3.0 | https://github.com/FDH2/UxPlay |
| uxplay-windows / libuxplay | GPL-3.0 | https://github.com/leapbtw/uxplay-windows (tag `2.0.0.1736`), https://github.com/leapbtw/libuxplay |
| GStreamer and plugins | LGPL-2.1-or-later | https://gstreamer.freedesktop.org |
| FFmpeg (libavcodec, libavformat, …) | LGPL-2.1-or-later; GPL for builds that include x264/x265 | https://ffmpeg.org |
| x264, x265 | GPL-2.0-or-later | https://www.videolan.org/developers/x264.html, https://www.videolan.org/developers/x265.html |
| Qt 6 | LGPL-3.0 | https://www.qt.io/download-open-source |
| GLib, GObject, GIO | LGPL-2.1-or-later | https://gitlab.gnome.org/GNOME/glib |
| libplist | LGPL-2.1-or-later | https://github.com/libimobiledevice/libplist |
| OpenSSL | Apache-2.0 | https://www.openssl.org |
| mDNSResponder (Apple) | Apache-2.0 | https://github.com/apple-oss-distributions/mDNSResponder |
| Other libraries in `engine/` (codecs, fonts, text shaping, compression) | Their respective open-source licenses | See the uxplay-windows build manifest |

**Corresponding source.** The engine binaries were taken unchanged from the official uxplay-windows release
(`uxplay-windows.zip`, SHA-256 `9D3A51C15FC9DB857351195E7EB7BBB21700D9AE25D936A54BCF8536B62CCA18`).
The complete source code and build scripts for that release are available at the uxplay-windows repository at tag `2.0.0.1736`
and in the upstream projects listed above. If any of those sources become unavailable, open an issue in this repository
and the corresponding source will be provided.

iStream's own source code (`src/`) is licensed under GPL-3.0. See [LICENSE](LICENSE).

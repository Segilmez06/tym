# 🍉 TYM

[![Debug build](https://github.com/Segilmez06/tym/actions/workflows/debug.yml/badge.svg)](https://github.com/Segilmez06/tym/actions/workflows/debug.yml)
![GitHub release](https://img.shields.io/github/v/release/Segilmez06/tym?label=Release)

TYM is a cross-platform tool that renders image files in your terminal. It uses VT100 escape codes to display 24-bit true color images.

## Requirements

- Unicode supported monospace font
- VT100 compatible terminal emulator with true color support

## Download

Get the lastest version from [Releases](https://github.com/Segilmez06/tym/releases) page. You can also get a debug build from [Actions](https://github.com/Segilmez06/tym/actions/workflows/debug.yml) tab.

Some browsers may block download because they filter all files with .exe suffix even if they are plain text.

## Installation

No additional installation required.

If you want to execute TYM from a shell don't forget to update your `PATH` variable.

## Usage

```bash
tym [--help] [--version] <file> [<options>]
```

### Options

| Short form | Long form           | Default value       | Description |
| :--------: | :------------------ | :------------------ | :---------- |
| `-r`       | `--resampler`       | `MitchellNetravali` | Resampling algorithm for downsizing.
| `-l`       | `--list-resamplers` | `false`             | List available resampling algorithms.
| `-x`       | `--x-margin`        | `0`                 | Left margin size as characters. Shifts output to right.
| `-y`       | `--y-margin`        | `0`                 | Top margin size as characters. Shifts output to bottom.
| `-w`       | `--width`           | `0` (autosize)      | Output width as pixels.
| `-h`       | `--height`          | `0` (autosize)      | Output height as pixels.
| `-m`       | `--resize-method`   | `Contain`           | Resizing mode.
| `-f`       | `--fullscreen`      | `false`             | Use fullscreen.
| `-c`       | `--clear`           | `false`             | Clear downloaded cache folder.

The fullscreen option overrides margin and size arguments.

TYM uses special Unicode characters to improve image scaling. That causes each character to contain 2 pixels vertically. That means if you specify top margin as 3, it will shift the output by 3 characters which is equals to 6 pixels.

### Examples

View local image with default settings:

```bash
tym example.png
```

View remote image with default settings:

```bash
tym https://example.com/image.png
```

View remote file with and resize to 96x64:

```bash
tym https://example.com/image.png -w 96 -h 54
```

View local file with fullscreen and cover all buffer area:

```bash
tym example.png -f -m Cover
```

## Screenshots

![Screenshot](screenshots/screenshot-1.png)

## Building

This tool is built on .Net 7 so it requires .Net SDK version >= 7 while building.

### Optional

For AOT binary compilation, check [official documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot#prerequisites) for dependencies. This packages are only required while publishing. Skip this step if you're going to use JIT compiled binary.

## Contributing

You can create pull requests and issues to help development. Also starring the repo will give me motivation.

## Credits

Image library: [SixLabors ImageSharp](https://github.com/SixLabors/ImageSharp)


TYM created by Segilmez06
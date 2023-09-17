from sys import argv, stdout
from os import path, get_terminal_size
from PIL import Image, ImageOps

TOP_BLOCK = '\u2580'
BOTTOM_BLOCK = '\u2584'
EMPTY_BLOCK = '\u0020'

# override_list_values_from_start = lambda list1, list2: [list2[i] if i < len(list2) else list1[i] for i in range(len(list1))]

args = argv
# args = override_list_values_from_start([""] * 2, argv)

program_name = args[0]
# args[1] = r"C:\Users\sarpe\Pictures\Sarp Logo\64\Circle.png"

help_msg = f"""
TYM - View your images in the terminal with true color

Usage: {program_name} <image_path>

This script requires:
- Unicode supported font
- VT100 terminal emulator with true color support
"""

if len(args) == 2:
    img_path = args[1]
    if path.exists(img_path):
        # with open(img_path, 'rb') as f:
        #     img = f.read()
        with Image.open(img_path) as img:
            w, h = get_terminal_size()
            w_target_pixel = int(w / 2)
            h_target_pixel = 40
            h_target_char = int(h_target_pixel / 2)
            resized = ImageOps.contain(img, (w_target_pixel, h_target_pixel))

            for y in range(int(resized.height / 2)):
                for x in range(resized.width):
                    p1_color = resized.getpixel((x, (y*2)))
                    p2_color = resized.getpixel((x, (y*2) + 1))

                    if p1_color[3] == 0 and p2_color[3] == 0:
                        stdout.write(f"\x1b[49m")
                        stdout.write(f"\x1b[39m")
                        stdout.write(EMPTY_BLOCK)
                    elif p2_color[3] == 0:
                        stdout.write(f"\x1b[38;2;{p1_color[0]};{p1_color[1]};{p1_color[2]}m")
                        stdout.write(f"\x1b[49m")
                        stdout.write(TOP_BLOCK)
                    elif p1_color[3] == 0:
                        stdout.write(f"\x1b[49m")
                        stdout.write(f"\x1b[38;2;{p2_color[0]};{p2_color[1]};{p2_color[2]}m")
                        stdout.write(BOTTOM_BLOCK)
                    else:
                        stdout.write(f"\x1b[38;2;{p1_color[0]};{p1_color[1]};{p1_color[2]}m")
                        stdout.write(f"\x1b[48;2;{p2_color[0]};{p2_color[1]};{p2_color[2]}m")
                        stdout.write(TOP_BLOCK)
                    stdout.write("\x1b[0m")

                print()
        exit(0)

print(help_msg)
exit(0)
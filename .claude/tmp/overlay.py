import struct, sys, os
from PIL import Image
LRX, LRY = 60, 30
def parse_grid(path):
    b=open(path,'rb').read(); o=0
    def i32():
        nonlocal o; v=struct.unpack_from('<i',b,o)[0]; o+=4; return v
    def u32():
        nonlocal o; v=struct.unpack_from('<I',b,o)[0]; o+=4; return v
    tileSize=i32(); mapH=i32(); mapW=i32(); tc=i32(); tds=u32(); mds=u32()
    o+=tc*8
    cols=(mapW+LRX-1)//LRX; rows=(mapH+LRY-1)//LRY
    grid=b[o:o+cols*rows]
    return mapW,mapH,cols,rows,grid
mid=sys.argv[1]
base=f"d:/GitProject/yu_client_unity/Assets/GameRes/resource/game/scene/map/{mid}"
mapW,mapH,cols,rows,grid=parse_grid(f"{base}/{mid}.bytes")
prev=Image.open(f"{base}/tile/{mid}.jpg").convert("RGB")
print("preview size",prev.size,"mapWxH",mapW,mapH,"grid",cols,rows)
# target render size
sc = max(1, 720//cols)
W,H = cols*sc, rows*sc
img = prev.resize((W,H)).convert("RGBA")
ov = Image.new("RGBA",(W,H),(0,0,0,0))
px = ov.load()
# grid[x=col][y=row]: index = col*rows + row
for col in range(cols):
    for row in range(rows):
        v = grid[col*rows+row]
        if v & 1:  # block
            for yy in range(row*sc,(row+1)*sc):
                for xx in range(col*sc,(col+1)*sc):
                    px[xx,yy]=(255,0,0,110)
out=Image.alpha_composite(img,ov).convert("RGB")
op=f"d:/GitProject/yu_client_unity/.claude/tmp/overlay_{mid}.png"
out.save(op); print("saved",op,out.size)
# also a side-by-side: left preview, right pure block map
pure=Image.new("RGB",(W,H),(255,255,255))
pp=pure.load()
for col in range(cols):
    for row in range(rows):
        if grid[col*rows+row]&1:
            for yy in range(row*sc,(row+1)*sc):
                for xx in range(col*sc,(col+1)*sc):
                    pp[xx,yy]=(0,0,0)
sbs=Image.new("RGB",(W*2+10,H),(128,128,128))
sbs.paste(img.convert("RGB"),(0,0)); sbs.paste(pure,(W+10,0))
sp=f"d:/GitProject/yu_client_unity/.claude/tmp/sbs_{mid}.png"
sbs.save(sp); print("saved",sp,sbs.size)

import struct, sys
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
cw,ch=6,3   # cell render size 2:1 to match 60:30
W,H=cols*cw,rows*ch
img=prev.resize((W,H)).convert("RGBA")
ov=Image.new("RGBA",(W,H),(0,0,0,0)); px=ov.load()
for col in range(cols):
    for row in range(rows):
        if grid[col*rows+row]&1:
            for yy in range(row*ch,(row+1)*ch):
                for xx in range(col*cw,(col+1)*cw):
                    px[xx,yy]=(255,0,0,120)
out=Image.alpha_composite(img,ov).convert("RGB")
out.save(f"d:/GitProject/yu_client_unity/.claude/tmp/ov2_{mid}.png")
pure=Image.new("RGB",(W,H),(255,255,255)); pp=pure.load()
for col in range(cols):
    for row in range(rows):
        if grid[col*rows+row]&1:
            for yy in range(row*ch,(row+1)*ch):
                for xx in range(col*cw,(col+1)*cw):
                    pp[xx,yy]=(0,0,0)
sbs=Image.new("RGB",(W*2+12,H),(120,120,120))
sbs.paste(img.convert("RGB"),(0,0)); sbs.paste(pure,(W+12,0))
sbs.save(f"d:/GitProject/yu_client_unity/.claude/tmp/sbs2_{mid}.png")
print("ok",mid,"render",W,H)

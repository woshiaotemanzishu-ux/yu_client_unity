import struct, glob, os
LRX,LRY=60,30
root="d:/GitProject/yu_client_unity/Assets/GameRes/resource/game/scene/map"
def parse(path):
    b=open(path,'rb').read(); o=0
    def i32():
        nonlocal o; v=struct.unpack_from('<i',b,o)[0]; o+=4; return v
    def u32():
        nonlocal o; v=struct.unpack_from('<I',b,o)[0]; o+=4; return v
    tileSize=i32(); mapH=i32(); mapW=i32(); tc=i32(); tds=u32(); mds=u32()
    o+=tc*8
    cols=(mapW+LRX-1)//LRX; rows=(mapH+LRY-1)//LRY
    gb=cols*rows; grid=b[o:o+gb]; o+=gb
    block=sum(1 for v in grid if v&1)
    resId=None; ac=0; blockAreaCells=0
    if len(b)-o>=4: resId=u32()
    if len(b)-o>=4:
        ac=u32()
        for _ in range(ac):
            if len(b)-o<12: break
            aid=u32(); at=u32(); pc=u32(); 
            if at==1: blockAreaCells+=pc
            o+=pc*8
    return mapW,mapH,gb,block,mds,resId,ac,blockAreaCells, (len(b)-o)
rows=[]
for f in glob.glob(root+"/*/*.bytes"):
    mid=os.path.basename(f)[:-6]
    try:
        mapW,mapH,gb,block,mds,resId,ac,bac,left=parse(f)
        pct=100.0*block/max(1,gb)
        rows.append((mid,mapW,mapH,gb,pct,mds==gb,resId,ac,bac,left))
    except Exception as e:
        rows.append((mid,"ERR",str(e),0,0,False,None,0,0,0))
# report anomalies
print("TOTAL maps:",len(rows))
print("\n-- maps with areaCount>0 --")
for r in sorted(rows,key=lambda r:-(r[7] if isinstance(r[7],int) else 0)):
    if isinstance(r[7],int) and r[7]>0:
        print(f"  {r[0]}: areas={r[7]} blockAreaCells={r[8]} block%={r[4]:.0f} size={r[1]}x{r[2]}")
print("\n-- maps with block% < 5 (suspicious: almost no static blocks) --")
for r in rows:
    if isinstance(r[4],float) and r[4]<5:
        print(f"  {r[0]}: block%={r[4]:.1f} size={r[1]}x{r[2]} gb={r[3]} mdsMatch={r[5]} left={r[9]}")
print("\n-- maps where maskDataSize != gridBytes (parse mismatch) --")
for r in rows:
    if r[5] is False and r[1]!='ERR':
        print(f"  {r[0]}: gb={r[3]} size={r[1]}x{r[2]}")
print("\n-- parse errors / nonzero leftover --")
for r in rows:
    if r[1]=='ERR' or (isinstance(r[9],int) and r[9]!=0):
        print(f"  {r[0]}: {r[1:]} ")

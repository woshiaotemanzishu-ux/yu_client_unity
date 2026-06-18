import struct, sys, os, glob

LRX, LRY = 60, 30

def parse(path):
    with open(path, 'rb') as f:
        b = f.read()
    o = 0
    def i32():
        nonlocal o
        v = struct.unpack_from('<i', b, o)[0]; o += 4; return v
    def u32():
        nonlocal o
        v = struct.unpack_from('<I', b, o)[0]; o += 4; return v
    tileSize = i32(); mapHeight = i32(); mapWidth = i32(); tileCount = i32()
    tileDataSize = u32(); maskDataSize = u32()
    o += tileCount * 8  # skip tiles (x,y) uint32 each
    cols = (mapWidth + LRX - 1) // LRX
    rows = (mapHeight + LRY - 1) // LRY
    gridBytes = cols * rows
    grid = b[o:o+gridBytes]; o += gridBytes
    # histogram of values + block bit count
    block = sum(1 for v in grid if (v & 1) != 0)
    nonzero = sum(1 for v in grid if v != 0)
    hist = {}
    for v in grid:
        hist[v] = hist.get(v, 0) + 1
    resId = None; areaCount = None; areas = []
    if len(b) - o >= 4:
        resId = u32()
    if len(b) - o >= 4:
        areaCount = u32()
        for _ in range(areaCount):
            if len(b) - o < 12: break
            aid = u32(); atype = u32(); pc = u32()
            o += pc * 8
            areas.append((aid, atype, pc))
    name = os.path.basename(path)
    print(f"== {name} ==")
    print(f"  tileSize={tileSize} mapW={mapWidth} mapH={mapHeight} tileCount={tileCount} tileDataSize={tileDataSize} maskDataSize={maskDataSize}")
    print(f"  grid cols={cols} rows={rows} gridBytes={gridBytes} (maskDataSize match? {gridBytes==maskDataSize})")
    print(f"  blockCells={block} ({100.0*block/max(1,gridBytes):.1f}%)  nonzeroCells={nonzero} ({100.0*nonzero/max(1,gridBytes):.1f}%)")
    top = sorted(hist.items(), key=lambda kv:-kv[1])[:8]
    print(f"  valueHist(top)={[(v,c) for v,c in top]}")
    print(f"  resId={resId} areaCount={areaCount}")
    blockAreas = [a for a in areas if a[1]==1]
    if areas:
        tcount = {}
        for _,t,_pc in areas: tcount[t]=tcount.get(t,0)+1
        print(f"  areaTypeCount={tcount}")
        print(f"  blockAreas(type==1)={len(blockAreas)} totalAreaCells={sum(a[2] for a in areas)} blockAreaCells={sum(a[2] for a in blockAreas)}")
    print(f"  leftoverBytes={len(b)-o}")

for p in sys.argv[1:]:
    for f in glob.glob(p):
        try:
            parse(f)
        except Exception as e:
            print(f"ERR {f}: {e}")

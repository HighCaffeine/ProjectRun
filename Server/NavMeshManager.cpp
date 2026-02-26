#include "NavMeshManager.h"
#include <stdio.h>

static const int NAVMESHSET_MAGIC = 'M' << 24 | 'S' << 16 | 'E' << 8 | 'T';
static const int NAVMESHSET_VERSION = 1;

static const int MAX_NODE = 2048;

static const int INCLUDE_FLAGS = 0xffff;
static const int EXCLUDE_FLAGS = 0;

static enum AreaID
{
    NONE = 0,
    Road = 1,
    Bush = 2,
}

struct NavMeshSetHeader
{
    int magic;
    int version;
    int numTiles;
    dtNavMeshParams params;
};

struct NavMeshTileHeader
{
    dtTileRef tileRef;
    int dataSize;
};

NavMeshManager::~NavMeshManager()
{
    if (m_NavMesh) dtFreeNavMesh(m_NavMesh);
    if (m_NavQuery) dtFreeNavMeshQuery(m_NavQuery);
}

bool NavMeshManager::Init(const string& binFilePath)
{
    FILE* fp = nullptr;
    fopen_s(&fp, binFilePath.c_str(), "rb");

    if (!fp) return false;

    NavMeshSetHeader header;
    fread(&header, sizeof(NavMeshSetHeader), 1, fp);

    if (header.magic != NAVMESHSET_MAGIC || header.version != NAVMESHSET_VERSION)
    {
        fclose(fp);
        return false;
    }

    m_NavMesh = dtAllocNavMesh();
    dtStatus status = m_NavMesh->init(&header.params);

    if (dtStatusFailed(status))
    {
        fclose(fp);
        return false;
    }

    for (int i = 0; i < header.numTiles; ++i)
    {
        NavMeshTileHeader tileHeader;
        fread(&tileHeader, sizeof(NavMeshTileHeader), 1, fp);

        if (!tileHeader.tileRef || !tileHeader.dataSize) break;

        unsigned char* data = (unsigned char*)dtAlloc(tileHeader.dataSize, DT_ALLOC_PERM);
        fread(data, tileHeader.dataSize, 1, fp);
        m_NavMesh->addTile(data, tileHeader.dataSize, DT_TILE_FREE_DATA, tileHeader.tileRef, 0);
    }

    fclose(fp);

    m_NavQuery = dtAllocNavMeshQuery();
    m_NavQuery->init(m_NavMesh, MAX_NODE);

    m_Filter.setIncludeFlags(INCLUDE_FLAGS);
    m_Filter.setExcludeFlags(EXCLUDE_FLAGS);

    return true;
}

bool NavMeshManager::GetValidMovePosition(const Vector3& startPos, const Vector3& targetPos, Vector3& realPos)
{
    if (!m_NavQuery) return false;

    float start[3] = { startPos.x, startPos.y, startPos.z };
    float target[3] = { targetPos.x, targetPos.y, targetPos.z };
    float result[3];

    dtPolyRef startRef;
    float nearStart[3];
    m_NavQuery->findNearestPoly(start, m_Extents, &m_Filter, &startRef, nearStart);

    //시작점이 맵 밖
    if (!startRef) return false;

    dtPolyRef visited[16];
    int visitedCount = 0;

    m_NavQuery->moveAlongSurface(startRef, start, target, &m_Filter, result, visited, &visitedCount, 16);

    float h = result[1];
    m_NavQuery->getPolyHeight(visited[visitedCount - 1], result, &h);
    result[1] = h;

    realPos = { result[0], result[1], result[2] };

    return true;
}

bool NavMeshManager::IsInBush(const Vector3& pos)
{
    if (!m_NavQuery) return false;

    float p[3] = { pos.x, pos.y, pos.z };
    dtPolyRef ref;
    float near[3];

    m_NavQuery->findNearestPoly(p, m_Extents, &m_Filter, &ref, near);
    if (!ref) return false;

    unsigned char areaID;
    m_NavMesh->getPolyArea(ref, &areaID);

    return (areaID == AreaID::Bush);
}

#pragma once
#include <string>
#include <iostream>

#include "Packet.h"

const UINT16 SERVER_PORT = 11021;
const UINT16 MAX_CLIENT = 3;		//총 접속할수 있는 클라이언트 수
const UINT32 MAX_IO_WORKER_THREAD = 4;  //쓰레드 풀에 넣을 쓰레드 수
const std::string SERVER_IP("40.82.137.214");
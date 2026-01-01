# CKFinalProject

CK Final Project Client

# MOONSCAR (월흔)

> "달의 파편이 꿰뚫은 해적들의 요새, 그 유쾌한 지옥에서의 사투"

## Project Overview

**MOONSCAR**는 6인 개인전으로 진행되는 **쿼터뷰 MOBA 배틀로얄** 게임입니다. 10분 내외의 짧고 강렬한 플레이 타임 속에서 전략적 이동과 빈번한 교전, 그리고 '영혼석'을 둘러싼 심리전을 제공합니다.

- **개발 기간:** 2026. 01. 01 ~ 2026. 11 (약 11개월)
- **장르:** 6-Player MOBA Battle Royale
- **플랫폼:** PC
- **엔진:** Unity 6000.3.2f1 LTS

---

## Key Features

- **유쾌한 지옥 (Pleasant Hell):** 해적 요새의 폐허와 달의 파편이 어우러진 독창적인 세계관.
- **달의 무덤 (Tomb of the Moon):** 맵 중앙의 분지 지형에서 펼쳐지는 핵심 오브젝트 점령전.
- **영혼석 성장 시스템:** 적과 몬스터를 처치해 얻은 영혼석으로 실시간 증강 스탯 선택.
- **전략적 지형지물:** 시야를 차단하는 '부시'와 리스크를 동반한 도주 경로인 '낭떠러지'.
- **버닝 모드 (Burning Mode):** 탈락 위기의 플레이어에게 부여되는 강력한 역전의 기회.

---

## Tech Stack

### Client

- **Engine:** Unity 6000 (LTS)
- **Rendering:** DirectX 12 Optimized
- **Audio:** Wwise Integration
- **Asset Management:** Addressables System

### Server & Infrastructure

- **Language:** C++
- **Network:** IOCP (Event-driven), UDP/TCP Hybrid
- **Database:** Redis (In-memory Data Store)
- **Protocol:** Google Protocol Buffers
- **Cloud:** Azure Cloud

---

## Team Organization

총 15명의 파트별 전문가로 구성된 팀입니다.

- **Design:** 시스템 기획(2), 콘텐츠 기획(1)
- **Programming:** 서버(1), 클라이언트(1)
- **Art:** 2D 원화(4), 캐릭터 모델링(2), 배경 모델러(3), 이펙트(1)

---

## Roadmap

1. **Prototype (Jan - Feb):** 로컬 환경 조작감 및 레벨 디자인 검증
2. **Vertical Slice (Mar - Apr):** 핵심 구역 최종 퀄리티 및 서버 동기화 완성
3. **Alpha (May - Jul):** 전체 리소스 양산 및 전체 시스템 구현
4. **Beta (Aug - Sep):** 사운드(Wwise) 통합 및 폴리싱, 밸런스 조정
5. **Final (Oct - Nov):** 안정화 테스트 및 2026 크로니클 출품 준비

---

© 2026 Team MOONSCAR. All rights reserved.

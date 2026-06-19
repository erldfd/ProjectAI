# Review: Summon Marking System

## Review Details
- **Reviewer**: Reviewer Agent & Tester Agent
- **Target**: Mark Target (Summon Combat) System - HFSM Init Bugfix

## Major Fixes
- `MonsterPeaceState.cs` & `MonsterCombatState.cs`: Resolved missing brace (`{}`) convention violations in `Enter()`.
- `NetMonsterBrain.cs`: Added defense logic (`Assert`) against NullReferenceException in `OnNetworkSpawn` if no root states are found during FSM setup.
- `MarkTargetSkillConfig.cs` & `MarkTargetSkillLogic.cs`: Added `TargetLayer` `LayerMask` field to allow targeted `BoxCast` filtering, significantly optimizing physics calculation performance and preventing non-monster layers from overflowing the buffer.

## Minor Fixes
- `NetSummonController.cs`: Added `TryGetComponent` fallback warning to explicitly notify developers if a non-AI entity is registered as a summon, preventing silent failures.

## Tester Deep Validation Fixes
- `MonsterPeaceState.cs` & `MonsterCombatState.cs`: Resolved a **Double Exit** logic flaw where re-entering a composite state would incorrectly trigger `Exit()` twice on the sub-states due to misuse of `ChangeState`. Replaced with `base.Enter()`.
- `NetMonsterBrain.cs`: Overrode `OnNetworkDespawn` to cleanly trigger `StateMachine.CurrentState?.Exit()` when the monster is returned to the NGO object pool. This ensures that lingering state logic (timers, events) doesn't corrupt subsequent respawns, which was the root cause of the previous unpredictable NREs.

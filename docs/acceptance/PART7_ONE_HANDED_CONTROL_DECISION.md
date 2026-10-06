# One-handed control decision

## Existing behavior confirmed

- Settings mirror the standard combat buttons for left-handed play; pause and ping remain in the system corner.
- HUD Workspace permits button position, size, and opacity customization with a reset path.
- The touch-input contract assigns movement and look to opposite screen zones and requires simultaneous move, aim, and fire.
- Gyro aim is optional and is only exposed when the device reports a gyro.

## Unmet rule

Part 7.2 calls for a one-handed layout. The current handedness toggle and workspace do not implement that behavior: a single-thumb input cannot simultaneously own the separate movement and look zones under the current contract, and gyro is not universally available.

## Decision needed before implementation

Choose the one-handed interaction model (for example, whether it relaxes simultaneous move/aim/fire, requires gyro, or changes the movement/look gesture contract). Do not silently change combat input semantics or describe a mirrored two-handed layout as one-handed support. Then define its control placement, persistence, and device acceptance conditions.

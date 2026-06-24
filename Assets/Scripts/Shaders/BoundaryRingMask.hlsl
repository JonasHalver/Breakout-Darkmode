//UNITY_SHADER_NO_UPGRADE
#ifndef BOUNDARY_RING_MASK_INCLUDED
#define BOUNDARY_RING_MASK_INCLUDED

#define MAX_BOUNDARY_HITS 16

// Set from C# with MaterialPropertyBlock:
//
// _BoundaryHits[i].xyz = collision world position
// _BoundaryHits[i].w   = collision start time
//
// _BoundaryHitCount = number of valid hits in the array
float4 _BoundaryHits[MAX_BOUNDARY_HITS];
float _BoundaryHitCount;

float BoundaryRingSingleHit_float(
    float2 surfacePosition,
    float2 hitPosition,
    float hitTime,
    float currentTime,
    float ringDuration,
    float ringSpeed,
    float ringWidth,
    float rippleFrequency,
    float rippleStrength)
{
    float age = currentTime - hitTime;

    // Kill hits that have not started or have expired.
    float alive = step(0.0, age) * step(age, ringDuration);

    // Avoid division by zero.
    float safeDuration = max(ringDuration, 0.0001);
    float safeWidth = max(ringWidth, 0.0001);

    // Strong at impact, fades out.
    float fade = 1.0 - saturate(age / safeDuration);
    fade *= fade;

    float distanceFromHit = distance(surfacePosition, hitPosition);
    float ringRadius = age * ringSpeed;

    // Main expanding ring.
    float distanceFromRing = abs(distanceFromHit - ringRadius);

    float mainRing =
        1.0 - smoothstep(safeWidth, safeWidth * 1.75, distanceFromRing);

    // Smaller water-like ripple detail near the main ring.
    float rippleWindow =
        1.0 - smoothstep(safeWidth, safeWidth * 5.0, distanceFromRing);

    float rippleWave =
        sin((distanceFromHit - ringRadius) * rippleFrequency) * 0.5 + 0.5;

    float rippleRing =
        rippleWave * rippleWindow * rippleStrength;

    float mask = max(mainRing, rippleRing);

    return mask * fade * alive;
}

void BoundaryRingMask_float(
    float3 WorldPosition,
    float CurrentTime,
    float RingDuration,
    float RingSpeed,
    float RingWidth,
    float RippleFrequency,
    float RippleStrength,
    out float RingMask)
{
#if defined(SHADERGRAPH_PREVIEW)
    RingMask = 0.0;
#else
    float totalMask = 0.0;

    // Your Breakout playfield is on the XY plane.
    // If you move the game to XZ later, change this to WorldPosition.xz.
    float2 surfacePosition = WorldPosition.xy;

    [unroll]
    for (int i = 0; i < MAX_BOUNDARY_HITS; i++)
    {
        if (i >= (int)_BoundaryHitCount)
        {
            break;
        }

        float4 hit = _BoundaryHits[i];

        totalMask += BoundaryRingSingleHit_float(
            surfacePosition,
            hit.xy,
            hit.w,
            CurrentTime,
            RingDuration,
            RingSpeed,
            RingWidth,
            RippleFrequency,
            RippleStrength
        );
    }

    RingMask = saturate(totalMask);
#endif
}

// Half precision wrapper.
// This helps if the graph/node is set to half precision.
void BoundaryRingMask_half(
    half3 WorldPosition,
    half CurrentTime,
    half RingDuration,
    half RingSpeed,
    half RingWidth,
    half RippleFrequency,
    half RippleStrength,
    out half RingMask)
{
    float result;

    BoundaryRingMask_float(
        float3(WorldPosition),
        (float)CurrentTime,
        (float)RingDuration,
        (float)RingSpeed,
        (float)RingWidth,
        (float)RippleFrequency,
        (float)RippleStrength,
        result
    );

    RingMask = (half)result;
}

#endif
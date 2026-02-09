#ifndef YPIPELINE_MACROS_INCLUDED
#define YPIPELINE_MACROS_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Lights and Shadows Related
// ----------------------------------------------------------------------------------------------------

#define MAX_DIRECTIONAL_LIGHT_COUNT         1 // Only Support One Directional Light - Sunlight
#define MAX_CASCADE_COUNT                   4
#define MAX_PUNCTUAL_LIGHT_COUNT            256
#define MAX_SHADOWING_SPOT_LIGHT_COUNT      64
#define MAX_SHADOWING_POINT_LIGHT_COUNT     12

#define MAX_REFLECTION_PROBE_COUNT          16

#define MAX_LIGHT_COUNT_PER_TILE            32
#define MAX_REFLECTION_PROBE_COUNT_PER_TILE 4

#define POINT_LIGHT 1
#define SPOT_LIGHT 2

#endif
#ifndef TYCOON_COASTAL_SHORE_INCLUDED
#define TYCOON_COASTAL_SHORE_INCLUDED
// Khớp mép lưới cát/nước trong CityCoastWaterMesh, tránh bọt chạy qua đất hoặc lộ khe bờ.
float CoastalShoreOffset(float z)
{
    return .6 * sin(z * .06) + .25 * sin(z * .13);
}
#endif

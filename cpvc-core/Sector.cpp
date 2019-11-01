#include "Sector.h"

Sector::Sector()
{
    _track = 0;
    _side = 0;
    _id = 0;
    _size = 0;
    _fdcRegister1 = 0;
    _fdcRegister2 = 0;
    _dataLength = 0;
    _data.clear();
}

Sector::Sector(const Sector& sector)
{
    _track = sector._track;
    _side = sector._side;
    _id = sector._id;
    _size = sector._size;
    _fdcRegister1 = sector._fdcRegister1;
    _fdcRegister2 = sector._fdcRegister2;

    _dataLength = sector._dataLength;
    _data.clear();
    for (size_t i = 0; i < sector._data.size(); i++)
    {
        _data.push_back(sector._data.at(i));
    }
}

Sector::~Sector()
{
}

word Sector::DataLength()
{
    return (word)_data.size();
}

StreamWriter& operator<<(StreamWriter& s, const Sector& sector)
{
    s << sector._track;
    s << sector._side;
    s << sector._id;
    s << sector._size;
    s << sector._fdcRegister1;
    s << sector._fdcRegister2;
    s << sector._dataLength;
    s << sector._data;

    return s;
}

StreamReader& operator>>(StreamReader& s, Sector& sector)
{
    s >> sector._track;
    s >> sector._side;
    s >> sector._id;
    s >> sector._size;
    s >> sector._fdcRegister1;
    s >> sector._fdcRegister2;
    s >> sector._dataLength;
    s >> sector._data;

    return s;
}

#pragma once

#include <string>

namespace comclassactivation
{
    public ref class Xbox sealed
    {
        public:
        void RecursiveCopyDirectory(Platform::String^ sourcePath, Platform::String^ destinationPath);
        void RecursiveDeleteDirectory(Platform::String^ path);
    };
}
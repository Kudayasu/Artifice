#include "Xbox.h"

using namespace comclassactivation;
using namespace Platform;

void Xbox::RecursiveCopyDirectory(Platform::String^ sourcePath, Platform::String^ destinationPath)
{
	Microsoft::Xbox::Development::StorageUtilities::RecursiveCopyDirectory(sourcePath, destinationPath);
}

void Xbox::RecursiveDeleteDirectory(Platform::String^ path)
{
	Microsoft::Xbox::Development::StorageUtilities::RecursiveDeleteDirectory(path);
}
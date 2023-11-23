all: build

clean:
	@dotnet clean

restore:
	@dotnet restore

build: clean restore
	@dotnet build

package-win:
	@mkdir dist
	@cmd /c copy /y "bin\Debug\netstandard2.1\0Harmony.dll" "dist\"
	@cmd /c copy /y "bin\Debug\netstandard2.1\ExtendedRoadUpgrades.dll" "dist\"
	@echo Packaged to dist/

package-unix: build
	@mkdir dist
	@cp bin/Debug/netstandard2.1/0Harmony.dll dist
	@cp bin/Debug/netstandard2.1/ExtendedRoadUpgrades.dll dist
	@echo Packaged to dist/
# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
### Changed
### Deprecated
### Removed
### Fixed

## [0.0.3] - 2019-11-05
### Added
- Add MappingRange element to AxisToFloatMapping
Full = Map to full (-1..1) range  
High = Map to high (0..1) range  
Low = Map to low (-1..0) range
- Implement Throttle Menu button support
### Changed
- Previous button states checked to stop repeated spamming of button down / up events
- Streamline of code, reduce number of XML tags
### Deprecated
### Removed
### Fixed
- TGP targetting now works  
When mapping POV to thumbstick of Joystick, on center of POV stick, release signal is sent

## [0.0.2] - 2019-09-15
### Added
- Mapping of the Throttle is now supported
- All controls on the Stick and Throttle should be mappable

## [0.0.1] - 2019-09-15
### Added
- Initial Release

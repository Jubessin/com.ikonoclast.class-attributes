# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.2.1] --- 2023-04-15

### Changed

- RequireTag enforcer now checks if tag is built-in
- added additional test for require tag attribute
- address issue with closing editor window using 'ESC' key
- address issue with editor window throwing error after compiling changes
- update package dependencies

## [2.2.0] --- 2023-02-21

### Added

- additional configurations for all enforcers
- RequireTag class attribute, class attribute enforcer, and tests

### Changed

- RequireLayer class attribute can create layers if not defined
- com.ikonoclast.common dependency version


## [2.1.1] --- 2023-01-31

### Changed

- com.ikonoclast.common dependency version
- exposed ID getter in ClassAttributeEnforcers

## [2.1.0] --- 2023-01-31

### Added

- getter/setter utility methods for specific class attr. enforcer enabled state

### Changed

- com.ikonoclast.common dependency version
- editor .asmdef platform is now editor-only

## [2.0.1] --- 2023-01-29

### Changes

- address FileNotFound exception in event of missing .configuration.json file.

## [2.0.0] --- 2023-01-29

### Added

- editor window for configuring class attribute enforcers enabled state
- added dependency to com.ikonoclast.common package, version 2.1.0

## [1.0.1]

## Changed

- copyright holder
- package author

## [1.0.0]

### Added

- class attributes
- class attribute enforcers

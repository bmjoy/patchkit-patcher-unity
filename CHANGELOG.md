# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [3.1.0]
### Added
- Sending logs to S3 on error
- Error if there's no enough space on the device
- Security check against application secret

## [3.0.2]
### Added
- Info how to fix JSON dll issue
- Asset Store tools and internal tools

### Changed
- Forcing torrent-client executable flag before start

### Fixed
- TorrentDownloader: Possible NullReferenceException on Dispose()

## [3.0.1]
### Fixed
- Linux application were not started correctly
- Version info tags reading
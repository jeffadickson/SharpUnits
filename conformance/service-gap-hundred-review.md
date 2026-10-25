# Java service/catalog gap implementation

Baseline: `2cc3f49`; 962 passing .NET tests. Both GitHub workflows are disabled.

This batch inventories 100 missing implementation obligations: 44 unit definitions
in the Java-style catalog, 33 quantity-type-to-system-unit bindings and 23 service
contracts. Each iteration targets one stable JP-SPI requirement. These are finer
than TCK assertions; 100 iterations do not mean 100 independently closed assertions.

The profile will supply built-in provider discovery, supported SIMPLE formatting,
system lookups, immutable catalogs, typed factory metadata and prefix discovery.
Automatic external assembly discovery, arbitrary Java Number storage and unsupported
format families remain explicit gaps. Existing native registries and APIs keep their
contracts. Compilation and full tests run every ten items; Java executes independently.

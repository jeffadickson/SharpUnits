-- SharpUnits Teal Type Declarations (tl >= 0.15)
-- Enforces ISO 80000 affine point-vs-displacement safety at Teal compile time.

global record SharpUnits
    record SymbolicUnit
        symbol: string
        ascii_symbol: string
        scale_to_si: number

        pow: function(SymbolicUnit, integer): SymbolicUnit
        root: function(SymbolicUnit, integer): SymbolicUnit
        to_si: function(SymbolicUnit): SymbolicUnit

        metamethod __mul: function(SymbolicUnit, SymbolicUnit): SymbolicUnit
        metamethod __div: function(SymbolicUnit, SymbolicUnit): SymbolicUnit
        metamethod __eq: function(SymbolicUnit, SymbolicUnit): boolean
        metamethod __tostring: function(SymbolicUnit): string
    end

    record Quantity
        value: number
        si_value: number
        unit: SymbolicUnit
        spec: string

        in_unit: function(Quantity, string | SymbolicUnit): Quantity
        to_si: function(Quantity): Quantity
        format: function(Quantity, string): string
        is_equivalent: function(Quantity, Quantity, number): boolean

        metamethod __add: function(Quantity, Quantity): Quantity
        metamethod __sub: function(Quantity, Quantity): Quantity
        metamethod __unm: function(Quantity): Quantity
        metamethod __mul: function(Quantity, Quantity | number): Quantity
        metamethod __div: function(Quantity, Quantity | number): Quantity
        metamethod __lt: function(Quantity, Quantity): boolean
        metamethod __le: function(Quantity, Quantity): boolean
        metamethod __eq: function(Quantity, Quantity): boolean
        metamethod __tostring: function(Quantity): string
    end

    record QuantityPoint
        value: number
        unit: SymbolicUnit
        origin: string

        in_unit: function(QuantityPoint, string | SymbolicUnit): QuantityPoint
        relative_to: function(QuantityPoint, string): QuantityPoint
        shift: function(QuantityPoint, Quantity): QuantityPoint
        to_celsius: function(QuantityPoint): QuantityPoint
        to_fahrenheit: function(QuantityPoint): QuantityPoint
        to_kelvin: function(QuantityPoint): QuantityPoint

        -- Compile-time affine safety:
        --   Point + Quantity  -> QuantityPoint (valid shift)
        --   Point - Point     -> Quantity      (valid displacement)
        --   Point + Point     -> COMPILE ERROR in tl check!
        metamethod __add: function(QuantityPoint, Quantity): QuantityPoint
        metamethod __sub: function(QuantityPoint, QuantityPoint): Quantity
        metamethod __lt: function(QuantityPoint, QuantityPoint): boolean
        metamethod __le: function(QuantityPoint, QuantityPoint): boolean
        metamethod __eq: function(QuantityPoint, QuantityPoint): boolean
        metamethod __tostring: function(QuantityPoint): string
    end

    record QuantitySeries
        count: integer
        unit: SymbolicUnit

        in_unit: function(QuantitySeries, string | SymbolicUnit): QuantitySeries
        auto_scale: function(QuantitySeries): QuantitySeries
        relative_to: function(QuantitySeries, string): QuantitySeries
        min: function(QuantitySeries): Quantity
        max: function(QuantitySeries): Quantity
        mean: function(QuantitySeries): Quantity
        rms: function(QuantitySeries): Quantity
        peak_to_peak: function(QuantitySeries): Quantity
        at: function(QuantitySeries, integer): Quantity

        metamethod __len: function(QuantitySeries): integer
    end

    unit: function(expr: string): SymbolicUnit
    qty: function(value: number, unit_expr: string): Quantity
    parse: function(literal: string): Quantity
    point: function(value: number, unit_expr: string, origin?: string): QuantityPoint
    series: function(values: {number}, unit_expr: string, origin?: string): QuantitySeries
end

return SharpUnits

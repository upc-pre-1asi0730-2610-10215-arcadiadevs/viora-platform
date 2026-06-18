using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Domain service that accumulates winter chill from hourly weather.
/// </summary>
public class ChillAccumulationCalculator
{
    private const double ChillingHoursLowerCelsius = 0.0;
    private const double ChillingHoursUpperCelsius = 7.2;

    // Dynamic Model constants (Fishman & Erez 1987; Luedeling et al. 2009 / chillR).
    private const double E0 = 4153.5;
    private const double E1 = 12888.8;
    private const double A0 = 139500.0;
    private const double A1 = 2.567e18;
    private const double Slope = 1.6;
    private const double Tf = 277.0;
    private const double Aa = A0 / A1;
    private const double Ee = E1 - E0;
    private const double CelsiusToKelvin = 273.0;

    /// <summary>
    ///     Accumulates the chill contributed by a window of hourly weather, continuing
    ///     the Dynamic Model from a previous carry-over state.
    /// </summary>
    public ChillAccumulation Accumulate(WeatherHistory history, ChillModelState incomingState)
    {
        if (history == null)
        {
            throw new ArgumentNullException(nameof(history), "Weather history is required to compute chill.");
        }

        var state = incomingState ?? ChillModelState.Empty();

        double chillHours = 0.0;
        double chillPortions = 0.0;
        double intermediate = state.IntermediateProduct;
        double? fromTemperature = state.PreviousHourTemperatureCelsius;
        double? priorTemperature = state.PriorHourTemperatureCelsius;

        foreach (var reading in history.Readings)
        {
            double temperature = reading.TemperatureCelsius;

            if (temperature >= ChillingHoursLowerCelsius && temperature <= ChillingHoursUpperCelsius)
            {
                chillHours += 1.0;
            }

            if (!fromTemperature.HasValue)
            {
                // First hour of the whole accumulation: establish the landing point.
                fromTemperature = temperature;
                continue;
            }

            double fromXs = Xs(fromTemperature.Value);
            double fromEak1 = Eak1(fromTemperature.Value);
            double fromXi = Xi(fromTemperature.Value);
            double resetXi = priorTemperature.HasValue ? Xi(priorTemperature.Value) : fromXi;

            double carried = intermediate >= 1.0 ? intermediate * (1.0 - resetXi) : intermediate;
            double next = fromXs - (fromXs - carried) * fromEak1;
            
            if (next >= 1.0)
            {
                chillPortions += next * fromXi;
            }

            intermediate = next;
            priorTemperature = fromTemperature;
            fromTemperature = temperature;
        }

        return new ChillAccumulation(
            chillHours,
            chillPortions,
            new ChillModelState(intermediate, fromTemperature, priorTemperature)
        );
    }

    private double Tk(double temperatureCelsius)
    {
        return temperatureCelsius + CelsiusToKelvin;
    }

    private double Xi(double temperatureCelsius)
    {
        double sr = Math.Exp(Slope * Tf * (Tk(temperatureCelsius) - Tf) / Tk(temperatureCelsius));
        return sr / (1.0 + sr);
    }

    private double Xs(double temperatureCelsius)
    {
        return Aa * Math.Exp(Ee / Tk(temperatureCelsius));
    }

    private double Eak1(double temperatureCelsius)
    {
        return Math.Exp(-A1 * Math.Exp(-E1 / Tk(temperatureCelsius)));
    }
}

/// <summary>
///     Chill accumulated over a window of hourly weather.
/// </summary>
public record ChillAccumulation(
    double ChillHours,
    double ChillPortions,
    ChillModelState NewState
);

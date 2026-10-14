# V14 CPU sensor availability

Audit date: 2026-10-10. Scope: read-only local interface probes on the Ryzen 7 8845H
system used for the V14 live tour. No driver, tool, setting, process, or Player was changed.

## Measurement context

`work/fps-hardware-20261010/measure.ps1` samples whole-system CPU usage and
`Win32_PerfFormattedData_Counters_ProcessorInformation` `_Total` frequency. It
deliberately stores `cpuActualClockMHz` and `cpuTemperatureCelsius` as null. Existing
V13 and V14 reports likewise leave actual CPU clock and CPU temperature unavailable.
The V14 rendered short run is documented in
`work/fps-hardware-20261010/RENDERED_MEASUREMENT_081545.md`; existing raw directories
include `samples-20261010T081545419Z` and `samples-20261010T085522643Z`.

The measurement contract requires genuine sensor values, not estimates. A reading
obtained now could only support a newly synchronized run; it cannot reconstruct CPU
temperature or clock history for a completed run.

## Probes and results

- `Win32_Processor` identified `AMD Ryzen 7 8845H w/ Radeon 780M Graphics`, 8 cores,
  16 logical processors, `CurrentClockSpeed=3801`, and `MaxClockSpeed=3801` MHz.
  This is the same Windows-reported value already disclaimed by the measurement notes;
  it is not measured live effective clock, per-core boost, or CPU temperature.
- Read `root\WMI:ProcessorPerformance` instances for all 16 logical processors.
  Every instance reported `frequency=3801`; the WMI performance fields were present,
  but do not provide an independently measured effective clock. They are not used as
  CPU sensor evidence.
- Enumerated `root\WMI` thermal, temperature, clock, processor, and vendor class names.
  `MSAcpi_ThermalZoneTemperature` exists as a class, but querying it returned zero
  instances. No ACPI temperature was available to interpret as a CPU package sensor.
- Queried `root\WMI:LENOVO_CPU_OVERCLOCKING_DATA`; no instance was returned. The class
  name alone does not establish available telemetry.
- Checked running processes and installed program directories for HWiNFO,
  HWMonitor, OpenHardwareMonitor, LibreHardwareMonitor, Ryzen Master, and AIDA64.
  HWiNFO64 is installed at `C:\Program Files\HWiNFO64` with its manual and executable,
  but no HWiNFO sensor process was running. No other checked sensor monitor was found
  running or installed in those locations. Its live sensor data/shared-memory interface
  therefore was not available to this read-only probe.
- Checked relevant service names. AMD Crash Defender, AMD External Events Utility,
  Lenovo Fn and Function Keys, and Lenovo Vantage services were running. No checked
  HWiNFO/LibreHardware/WinRing/RTCore sensor access service was found. The running
  vendor services are not themselves evidence of CPU temperature or effective clocks.

## Finding and supported path forward

No genuine CPU temperature or live/effective CPU clock source was available from the
interfaces queried in this audit. Preserve both fields as null. Do not substitute the
3801 MHz Windows value, the WMI `ProcessorPerformance` frequency, a CPU base/maximum
specification, GPU temperature, or an ACPI zone value.

HWiNFO is a possible source for a future synchronized measurement because it is
installed, but its sensor mode and shared-memory data were not activated or validated
here. A future run would need an approved active HWiNFO sensor session, identification
of the CPU package temperature sensor and effective-clock fields, and timestamp-aligned
sampling with the Player. If those fields cannot be exposed without adding a driver,
retain null and report the limitation. Do not attribute any such future sensor reading
to prior V14 captures.

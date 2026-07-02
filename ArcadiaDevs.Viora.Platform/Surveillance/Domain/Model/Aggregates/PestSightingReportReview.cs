using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

public partial class PestSightingReport
{
    public void ConfirmAfterInspection()
    {
        if (Status is not (EReportStatus.UNDER_REVIEW or EReportStatus.NEEDS_INSPECTION or EReportStatus.LOGGED))
        {
            throw new InvalidOperationException(
                $"Cannot confirm a report with status {Status}. Expected UNDER_REVIEW, NEEDS_INSPECTION, or LOGGED.");
        }
        Status = EReportStatus.CONFIRMED;
        AlertConfirmed = true;
    }

    public void DismissAfterInspection()
    {
        if (Status is not (EReportStatus.UNDER_REVIEW or EReportStatus.NEEDS_INSPECTION or EReportStatus.LOGGED))
        {
            throw new InvalidOperationException(
                $"Cannot dismiss a report with status {Status}. Expected UNDER_REVIEW, NEEDS_INSPECTION, or LOGGED.");
        }
        Status = EReportStatus.RULED_OUT;
        AlertConfirmed = false;
    }
}

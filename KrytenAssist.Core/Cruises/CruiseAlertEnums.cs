namespace KrytenAssist.Core.Cruises;

public enum CruiseAlertType { PriceDrop, Promotion, SavedCriteria, CabinAvailability, NewItinerary }
public enum CruiseAlertStatus { Unread, Read, Dismissed }
public enum CruiseAlertEvidenceOrigin { RecordedObservation, SavedSnapshot }
public enum SavedCruiseCriteriaResult { Unknown, NotMet, Met }
public enum CruiseCabinAlertDirection { BecameAvailable, BecameUnavailable }

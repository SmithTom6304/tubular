# Tubular

A live map of the London Underground: train positions and arrivals are polled
from TfL, streamed through Kafka, and rendered on a tube map in the browser.

## Language

**NaptanId**:
The unique identifier for a stop point, held as a value type (not a bare
string) so it compares and hashes reliably. Its content follows the **ATCO
Code** format.
_Avoid_: stop id, station id

**ATCO Code**:
The format `NaptanId` values are encoded in — `999ZXXXXXXXX`: three numeric
digits for the administrative area, one digit that is `0` or `1`, then one to
eight further alphanumeric characters. Total length is 5–12 characters, not a
fixed 12 — the padded `999ZXXXXXXXX` form is a display convention, not the
underlying constraint. The suffix alphabet is case-sensitive per the NaPTAN
schema (`A-Za-z0-9`), though every id seen from TfL so far is uppercase-only.
Defined by the NaPTAN/ATCO standard, not by this project.
_Avoid_: naptan code (that's the identifier `NaptanId` holds; ATCO Code is the
format it's encoded in)

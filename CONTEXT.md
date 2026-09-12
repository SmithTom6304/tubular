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

**StopPoint**:
A single physical location a train can call at — one platform, entrance, or
stand — identified by its `NaptanId`. Distinct from a **Station**, which
groups several StopPoints (e.g. the platforms and entrances sharing one
interchange) under a separate parent identifier. The relationship between the
two is not yet modelled — deferred until crowding aggregation needs it.
_Avoid_: station, stop (ambiguous — could mean StopPoint or Station)

**Train**:
A single physical vehicle in service, tracked across its journey. TfL's API
identifies it as `VehicleId`.
_Avoid_: vehicle (that's the TfL API field name, not the domain term)

**Arrival Prediction**:
TfL's forecast of when a specific Train will reach a specific StopPoint on a
specific Line, carried as an absolute timestamp. Superseded repeatedly as the
train approaches — not a record that an arrival happened. This is the event
on the Kafka arrival-predictions topic, the shared contract between the
poller and the simulator (see
[ADR-0005](docs/adr/0005-arrival-prediction-contract.md)).
_Avoid_: arrival, arrival event (both read as "the train has arrived", not
"TfL predicts it will")

**Line**:
A London Underground service line (e.g. Jubilee) that a StopPoint sits on and
a Train runs along. Currently a plain string (TfL's own lowercase id form,
e.g. `"jubilee"`), not a value type — the set of lines is genuinely closed,
but nothing enforces that yet. See
[ADR-0005](docs/adr/0005-arrival-prediction-contract.md#update-2026-09-12-line-joined-the-grain).
_Avoid_: route (TfL's API uses "route" for something more specific — a
sequence of StopPoints — not a line as a whole)

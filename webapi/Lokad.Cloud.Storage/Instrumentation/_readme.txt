Lokad.Cloud.Storage Instrumentation
===================================

Intention
---------

A pure and robust distributed cloud application is expecting
transient errors of all kind when accessing remote resources.
Our experience with Windows Azure shows that this expectation
is well justified.

Lokad.Cloud.Storage tries to automatically handle transient
errors and retry where it makes sense. Similarly it automatically
tries to recover from exceptional cases like creating an blob
container when writing a blob to a container that does not exit yet
(which is non-trivial since a container could have been removed
by another party since the last check and containers can't be
recreated for a while just after they've been deleted).

The automatic error handling shields the developer from
most of the complexity, yet sometimes it would still be interesting
to have some insights into internal behaviour. Instead of writing
some obsucure logs, Lokad.Cloud.Storage optionally publishes some
system events to arbitrary observers.

Usage
-----

Storage events implement the ICloudStorageEvent marker interface.

You can observe storage events in the following ways:

*  Implementing the ICloudStorageObserver interface and pass
   it to the CloudStorage factory with the WithObserver method.

*  Implement IObserver<ICloudStorageEvent> one or more times
   and pass it to CloudStorage with the WithObservers method.

*  Register the CloudStorageInstrumentationSubject class
   to CloudStorage with the WithObserver method and consume it
   with Rx (it implements IObservable<ICloudStorageEvent> and
   behaves like an Rx subject).

Code Samples
------------

Lokad.Cloud.Framework currently implements a class that subscribes
the subject class using Rx to write warnings and debug messages
to the log: Lokad.Cloud.Diagnostics.CloudStorageLogger.
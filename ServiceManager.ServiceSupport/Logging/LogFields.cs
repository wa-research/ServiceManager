#region Copyright 2008-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;

namespace ServiceManager.ServiceSupport.Logging
{
	enum LogFields : int
	{
		/*RESERVED: this == 0*/
		[LogField(typeof(int))]
		EventId = 1,
		[LogField(typeof(DateTime))]
		EventTime,

		[LogField(typeof(int))]
		ProcessId,
		[LogField(typeof(string))]
		ProcessName,
		[LogField(typeof(string))]
		AppDomainName,
		[LogField(typeof(string))]
		EntryAssembly,
		[LogField(typeof(string))]
		ThreadPrincipalName,

		[LogField(typeof(int))]
		ManagedThreadId,
		[LogField(typeof(string))]
		ManagedThreadName,

		[LogField(typeof(int))]
		Level,
		[LogField(typeof(int))]
		Output,
		//this is special cased : [LogField(typeof(SharedException))]
		Exception,
		[LogField(typeof(string))]
		Message,

		[LogField(typeof(string))]
		FileName,
		[LogField(typeof(int))]
		FileLine,
		[LogField(typeof(int))]
		FileColumn,

		[LogField(typeof(string))]
		MethodAssemblyVersion,
		[LogField(typeof(string))]
		MethodAssembly,
		[LogField(typeof(string))]
		MethodType,
		[LogField(typeof(string))]
		MethodTypeName,

		[LogField(typeof(string))]
		MethodName,
		[LogField(typeof(string))]
		MethodArgs,
		[LogField(typeof(int))]
		IlOffset,
		//expressions: see implementation in EventDataFormatter
		FileLocation,
		Location,
		Method,
		FullMessage,

		[LogField(typeof(string[]))]
		LogStack,
		LogCurrent,
	}

	[AttributeUsage(AttributeTargets.Field)]
	class LogFieldAttribute : Attribute
	{
		public readonly Type SerializeType;
		public LogFieldAttribute(Type type) { SerializeType = type; }
	}
}

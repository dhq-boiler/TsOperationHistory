﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using TsOperationHistory.Internal;

namespace TsOperationHistory.Extensions
{
    public static class OperationControllerServiceExtensions
    {
        public static void ExecuteAdd<T>(this IOperationController controller, IList<T> list, T value)
        {
            var operation = list.ToAddOperation(value);
            controller.Execute(operation);
        }
        
        public static void ExecuteInsert<T>(this IOperationController controller, IList<T> list, T value , int index)
        {
            var operation = new InsertOperation<T>(@list, value , index);
            controller.Execute(operation);
        }
        
        public static void ExecuteAddRange<T>(this IOperationController controller, IList<T> list, IEnumerable<T> value )
        {
            var operation = list.ToAddRangeOperation(value);
            controller.Execute(operation);
        }
        
        public static void ExecuteRemove<T>(this IOperationController controller, IList<T> list, T value)
        {
            var operation = list.ToRemoveOperation(value);
            controller.Execute(operation);
        }
        
        public static void ExecuteRemoveAt<T>(this IOperationController controller, IList<T> list, int index)
        {
            if (list is IList iList)
            {
                var operation = iList.ToRemoveAtOperation(index);
                controller.Execute(operation);                
            }
            else
            {
                var target = list[index];
                var operation = list.ToRemoveOperation(target);
                controller.Execute(operation);
            }
        }
        
        public static void ExecuteRemoveItems<T>(this IOperationController controller, IList<T> list, IEnumerable<T> value )
        {
            var operation = list.ToRemoveRangeOperation(value);
            controller.Execute(operation);
        }

        public static void ExecuteSetProperty<T,TProperty>(this IOperationController controller, T owner , string propertyName , TProperty value)
        {
            var operation = owner
                .GenerateSetPropertyOperation(propertyName, value)
                .Merge(controller);
            
            controller.Execute(operation);
        }

        public static void ExecuteSetProperty<T, TProperty>(this IOperationController controller, Type @class, string propertyName, TProperty value)
        {
            var operation = @class.GenerateSetPropertyOperation(propertyName, value)
                                 .Merge(controller);

            controller.Execute(operation);
        }

        public static IDisposable BindPropertyChanged<T>(this IOperationController controller , INotifyPropertyChanged owner, string propertyName , bool autoMerge = true)
        {
            var prevValue = FastReflection.GetProperty<T>(owner, propertyName);
            var callFromOperation = false;
            owner.PropertyChanged += PropertyChanged;

            return new Disposer(() => owner.PropertyChanged -= PropertyChanged);

            // local function
            void PropertyChanged(object sender, PropertyChangedEventArgs args)
            {
                if (callFromOperation)
                    return;
                
                if (args.PropertyName == propertyName)
                {
                    callFromOperation = true;
                    T newValue = FastReflection.GetProperty<T>(owner, propertyName);
                    var operation = owner
                        .GenerateAutoMergeOperation(propertyName,newValue,prevValue,$"{sender.GetHashCode()}.{propertyName}",Operation.DefaultMergeSpan);

                    if (autoMerge)
                    {
                        operation = operation.Merge(controller);
                    }

                    operation
                        .AddPreEvent(() => callFromOperation = true)
                        .AddPostEvent(() => callFromOperation = false);
                    
                    prevValue = newValue;
                    
                    controller.Push(operation);
                    callFromOperation = false;
                }
            }
        }
    }
}
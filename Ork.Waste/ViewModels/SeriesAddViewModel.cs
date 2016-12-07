﻿#region License

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at
//  
// http://www.apache.org/licenses/LICENSE-2.0.html
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  
// Copyright (c) 2013, HTW Berlin

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using Caliburn.Micro;
using Ork.Framework;
using Ork.Waste.DomainModelService;
using Ork.Waste.Factories;
using DayOfWeek = System.DayOfWeek;

namespace Ork.Waste.ViewModels
{
  public class SeriesAddViewModel : Screen
  {
    protected readonly Series Model;
    private readonly ObservableCollection<SelectableContainerViewModel> m_Container = new ObservableCollection<SelectableContainerViewModel>();
    private readonly IContainerViewModelFactory m_ContainerViewModelFactory;
    private readonly IWasteRepository m_Repository;
    private readonly IResponsibleSubjectViewModelFactory m_ResponsibleSubjectViewModelFactory;
    protected readonly ObservableCollection<ResponsibleSubjectViewModel> m_ResponsibleSubjects = new ObservableCollection<ResponsibleSubjectViewModel>();
    private readonly ITaskGenerator m_TaskGenerator;
    private string m_ContainerSearchText = string.Empty;
    private string m_EmployeeSearchText = string.Empty;
    private ResponsibleSubjectViewModel m_SelectedResponsibleSubject;
    private IList<SelectableWeekdayViewModel> m_WeekDayList;

    public SeriesAddViewModel(Series model, IWasteRepository repository, IContainerViewModelFactory containerViewModelFactory, IResponsibleSubjectViewModelFactory responsibleSubjectViewModelFactory,
      ITaskGenerator taskGenerator)
    {
      DisplayName = TranslationProvider.Translate("TitleSeriesAddViewModel");
      Model = model;
      m_Repository = repository;
      m_ContainerViewModelFactory = containerViewModelFactory;
      m_ResponsibleSubjectViewModelFactory = responsibleSubjectViewModelFactory;
      m_TaskGenerator = taskGenerator;
      CreateSelectableContainerViewModels(repository.Container);
      CreateResponsibleSubjectViewModels(repository.ResponsibleSubjects);
    }

    public virtual bool IsEditable
    {
      get { return true; }
    }

    public DateTime BeginDate
    {
      get { return DueDateBegin; }
      set
      {
        DueDateBegin = new DateTime(value.Year, value.Month, value.Day, DueDateBegin.Hour, DueDateBegin.Minute, DueDateBegin.Second);
        NotifyOfPropertyChange(() => DueDateBegin);
        NotifyOfPropertyChange(() => DueDateEnd);
      }
    }

    public DateTime BeginTime
    {
      get { return DueDateBegin; }
      set { DueDateBegin = new DateTime(DueDateBegin.Year, DueDateBegin.Month, DueDateBegin.Day, value.Hour, value.Minute, value.Second); }
    }

    public Color Color
    {
      get
      {
        var col = new Color();
        col.A = 255;
        col.R = Model.SeriesColor.R;
        col.G = Model.SeriesColor.G;
        col.B = Model.SeriesColor.B;
        return col;
      }
      set
      {
        Model.SeriesColor.R = value.R;
        Model.SeriesColor.G = value.G;
        Model.SeriesColor.B = value.B;
      }
    }

    public string ContainerSearchText
    {
      get { return m_ContainerSearchText; }
      set
      {
        m_ContainerSearchText = value;
        NotifyOfPropertyChange(() => FilteredContainers);
      }
    }

    public int Cycle
    {
      get { return Model.Cycle; }
      set
      {
        Model.Cycle = value;
        NotifyOfPropertyChange(() => Cycle);
        NotifyOfPropertyChange(() => Range);
        if (value == 1 &&
            !WeekDayList.Any(wd => wd.IsSelected))
        {
          PreSelectCurrentWeekDay();
        }
        GetRepeatUntilDateValue();
      }
    }

    public int[] Days
    {
      get
      {
        return new[]
               {
                 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30
               };
      }
    }

    public DateTime DueDateBegin
    {
      get { return Model.Begin; }
      set
      {
        var range = Model.End.Subtract(Model.Begin);
        Model.Begin = value;
        Model.End = Model.Begin.Add(range);
        NotifyOfPropertyChange(() => DueDateBegin);
        NotifyOfPropertyChange(() => SeriesBeginDate);
        NotifyOfPropertyChange(() => RepeatUntilDate);
        NotifyOfPropertyChange(() => EndDate);
        NotifyOfPropertyChange(() => EndTime);
        NotifyOfPropertyChange(() => BeginDate);
        NotifyOfPropertyChange(() => BeginTime);
        NotifyOfPropertyChange(() => DueDateEnd);
        NotifyOfPropertyChange(() => GenerateEnabled);
        GetRepeatUntilDateValue();
      }
    }

    public DateTime DueDateEnd
    {
      get { return Model.End; }
      set
      {
        Model.End = value;
        if (Model.Begin >= Model.End)
        {
          Model.Begin = Model.End.Subtract(new TimeSpan(0, 30, 0));
        }

        NotifyOfPropertyChange(() => EndDate);
        NotifyOfPropertyChange(() => EndTime);
        NotifyOfPropertyChange(() => BeginDate);
        NotifyOfPropertyChange(() => BeginTime);
        NotifyOfPropertyChange(() => DueDateEnd);
        NotifyOfPropertyChange(() => DueDateBegin);
        NotifyOfPropertyChange(() => GenerateEnabled);
      }
    }

    public string EmployeeSearchText
    {
      get { return m_EmployeeSearchText; }
      set
      {
        m_EmployeeSearchText = value;
        NotifyOfPropertyChange(() => FilteredEmployees);
      }
    }

    public DateTime EndDate
    {
      get { return DueDateEnd; }
      set
      {
        DueDateEnd = new DateTime(value.Year, value.Month, value.Day, DueDateBegin.Hour, DueDateBegin.Minute, DueDateBegin.Second);
        NotifyOfPropertyChange(() => DueDateBegin);
        NotifyOfPropertyChange(() => DueDateEnd);
      }
    }

    public DateTime EndTime
    {
      get { return DueDateEnd; }
      set { DueDateEnd = new DateTime(DueDateBegin.Year, DueDateBegin.Month, DueDateBegin.Day, value.Hour, value.Minute, value.Second); }
    }

    public bool EndsWithDate
    {
      get { return Model.EndsWithDate; }
      set
      {
        Model.EndsWithDate = value;
        NotifyOfPropertyChange(() => EndsWithDate);
      }
    }

    public IEnumerable<SelectableContainerViewModel> FilteredContainers
    {
      get
      {
        return SearchInContainerList()
          .ToArray();
      }
    }

    public IEnumerable<object> FilteredEmployees
    {
      get
      {
        return SearchInEmployeeList()
          .ToArray();
      }
    }

    public bool GenerateEnabled
    {
      get
      {
        return (SelectedResponsibleSubject != null && FilteredContainers.Any(c => c.IsSelected) && (DueDateEnd > DueDateBegin) && (WeekDayList.Any(wd => wd.IsSelected) || Cycle != 1) &&
                !String.IsNullOrEmpty(Name));
      }
    }

    public bool IsAllDay
    {
      get { return Model.IsAllDay; }
      set
      {
        Model.IsAllDay = value;
        NotifyOfPropertyChange(() => IsAllDay);
      }
    }

    public bool IsWeekdayRecurrence
    {
      get { return Model.IsWeekdayRecurrence; }
      set
      {
        Model.IsWeekdayRecurrence = value;
        NotifyOfPropertyChange(() => IsWeekdayRecurrence);
      }
    }

    public string Name
    {
      get { return Model.Name; }
      set
      {
        Model.Name = value;
        NotifyOfPropertyChange(() => Name);
        NotifyOfPropertyChange(() => GenerateEnabled);
      }
    }


    public int NumberOfRecurrences
    {
      get { return Model.NumberOfRecurrences; }
      set
      {
        if (Model.NumberOfRecurrences == value)
        {
          return;
        }
        Model.NumberOfRecurrences = value;
        NotifyOfPropertyChange(() => NumberOfRecurrences);
      }
    }

    public int RecurrenceInterval
    {
      get { return Model.RecurrenceInterval; }
      set
      {
        Model.RecurrenceInterval = value;
        NotifyOfPropertyChange(() => RecurrenceInterval);
      }
    }


    public string Range
    {
      get
      {
        string uiString;
        var key = "";

        switch (Cycle)
        {
          case 0:
            key = key + "Days";
            break;
          case 1:
            key = key + "Weeks";
            break;
          case 2:
            key = key + "Months";
            break;
          case 3:
            key = key + "Years";
            break;
        }

        //var loc = new LocExtension(key);       

        //loc.ResolveLocalizedValue(out uiString);
        return TranslationProvider.Translate(key);
        //return uiString;
      }
    }

    public bool Repeat
    {
      get { return Model.Repeat; }
      set
      {
        if (Model.Repeat == value)
        {
          return;
        }
        Model.Repeat = value;
        NotifyOfPropertyChange(() => Repeat);
      }
    }

    public DateTime RepeatUntilDate
    {
      get { return Model.RepeatUntilDate; }
      set
      {
        Model.RepeatUntilDate = value;
        NotifyOfPropertyChange(() => RepeatUntilDate);
      }
    }

    public virtual ResponsibleSubjectViewModel SelectedResponsibleSubject
    {
      get { return m_SelectedResponsibleSubject; }
      set
      {
        if (m_SelectedResponsibleSubject == value)
        {
          return;
        }
        m_SelectedResponsibleSubject = value;
        NotifyOfPropertyChange(() => SelectedResponsibleSubject);
        NotifyOfPropertyChange(() => GenerateEnabled);
      }
    }

    public string SeriesBeginDate
    {
      get { return Model.Begin.ToString("ddddd, d MMMM yyyy"); }
    }

    public IEnumerable<SelectableWeekdayViewModel> WeekDayList
    {
      get
      {
        if (m_WeekDayList == null)
        {
          m_WeekDayList = new List<SelectableWeekdayViewModel>();
          foreach (var weekDay in new[]
                                  {
                                    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
                                  })
          {
            var swvm = new SelectableWeekdayViewModel(weekDay, false);
            swvm.PropertyChanged += WeekDaySelectionChanged;
            m_WeekDayList.Add(swvm);
          }
        }
        return m_WeekDayList;
      }
    }

    private void WeekDaySelectionChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "IsSelected")
      {
        NotifyOfPropertyChange(() => GenerateEnabled);
      }
    }

    public void GenerateReadings()
    {
      if (Repeat && Cycle == 1)
      {
        foreach (var swvm in WeekDayList.Where(wd => wd.IsSelected))
        {
          var dayOfWeek = new DomainModelService.DayOfWeek();
          dayOfWeek.WeekDay = (int) swvm.DayOfWeek;
          Model.WeekDays.Add(dayOfWeek);
        }
      }

      foreach (var scvm in m_Container.Where(scvm => scvm.IsSelected))
      {
        var result = m_TaskGenerator.GenerateFillLevelReadings(scvm.ContainerViewModel.Model, SelectedResponsibleSubject.Model, Model, WeekDayList.Where(wd => wd.IsSelected)
                                                                                                                                                  .Select(wd => wd.DayOfWeek)
                                                                                                                                                  .ToArray());

        foreach (var reading in result)
        {
          m_Repository.FillLevelReadings.Add(reading);
        }
      }
      m_Repository.Save();
      TryClose();
    }

    public void PreSelectCurrentWeekDay()
    {
      var currentWeekDay = DateTime.Now.DayOfWeek;
      foreach (var swvm in WeekDayList)
      {
        swvm.IsSelected = swvm.DayOfWeek == currentWeekDay;
      }
    }

    private void ContainerSelectionChanged(object sender, PropertyChangedEventArgs e)
    {
      NotifyOfPropertyChange(() => GenerateEnabled);
    }

    private void CreateResponsibleSubjectViewModels(IEnumerable<ResponsibleSubject> responsibleSubjects)
    {
      foreach (var responsibleSubject in responsibleSubjects)
      {
        if (responsibleSubject.IsOfType<Employee>())
        {
          m_ResponsibleSubjects.Add(m_ResponsibleSubjectViewModelFactory.CreateFromExisting((Employee) responsibleSubject));
        }
        else
        {
          m_ResponsibleSubjects.Add(m_ResponsibleSubjectViewModelFactory.CreateFromExisting((EmployeeGroup) responsibleSubject));
        }
      }
    }

    private void CreateSelectableContainerViewModels(IEnumerable<WasteContainer> containerModels)
    {
      foreach (var containerModel in containerModels)
      {
        var scvm = new SelectableContainerViewModel(m_ContainerViewModelFactory.CreateFromExisiting(containerModel), false);
        scvm.PropertyChanged += ContainerSelectionChanged;
        m_Container.Add(scvm);
      }
    }

    private void GetRepeatUntilDateValue()
    {
      switch (Cycle)
      {
        case 0:
          RepeatUntilDate = DueDateBegin.AddDays(28);
          break;
        case 1:
          RepeatUntilDate = DueDateBegin.AddWeeks(12);
          break;
        case 2:
          RepeatUntilDate = DueDateBegin.AddMonths(12);
          break;
        case 3:
          RepeatUntilDate = DueDateBegin.AddYears(5);
          break;
        default:
          RepeatUntilDate = DueDateBegin;
          break;
      }
    }

    private IEnumerable<SelectableContainerViewModel> SearchInContainerList()
    {
      if (string.IsNullOrEmpty(ContainerSearchText))
      {
        return m_Container;
      }
      var searchText = ContainerSearchText.ToLower();
      var searchResult = m_Container.Where(c => (((c.ContainerViewModel.Name != null) && (c.ContainerViewModel.Name.ToLower()
                                                                                           .Contains(searchText))) || ((c.ContainerViewModel.Barcode != null) && (c.ContainerViewModel.Barcode.ToLower()
                                                                                                                                                                   .Contains(searchText)))) ||
                                                ((c.ContainerViewModel.SelectedAvvWasteTypes != null) &&
                                                 (c.ContainerViewModel.SelectedAvvWasteTypes.Contains(c.ContainerViewModel.SelectedAvvWasteTypes.FirstOrDefault(wt => wt.Number.ToString()
                                                                                                                                                                        .Contains(searchText))))) ||
                                                ((c.ContainerViewModel.Map != null) && (c.ContainerViewModel.Map.Name.ToLower()
                                                                                         .Contains(searchText))));
      return searchResult;
    }

    private IEnumerable<object> SearchInEmployeeList()
    {
      if (string.IsNullOrEmpty(EmployeeSearchText))
      {
        return m_ResponsibleSubjects;
      }
      var searchText = EmployeeSearchText.ToLower();

      var searchResultEmployees = m_ResponsibleSubjects.OfType<EmployeeViewModel>()
                                                       .Where((e => (((e.FirstName != null) && (e.FirstName.ToLower()
                                                                                                 .Contains(searchText))) || ((e.LastName != null) && (e.LastName.ToLower()
                                                                                                                                                       .Contains(searchText)))) ||
                                                                    ((e.Number != null) && (e.Number.ToLower()
                                                                                             .Contains(searchText)))));
      var searchResultGroups = m_ResponsibleSubjects.OfType<GroupViewModel>()
                                                    .Where(g => (((g.Name != null) && (g.Name.ToLower()
                                                                                        .Contains(searchText)))));
      return searchResultGroups.Concat<object>(searchResultEmployees);
    }
  }
}
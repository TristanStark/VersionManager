VersionManager/
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
│
├── Models/
│   ├── VersionInfo.cs
│   ├── WorkflowStep.cs
│   ├── ZipEntryInfo.cs
│   ├── FileDetailInfo.cs
│   └── ApiStatusInfo.cs
│
├── Services/
│   ├── Interfaces/
│   │   ├── IApiService.cs
│   │   └── IZipService.cs
│   ├── ApiService.cs
│   └── ZipService.cs
│
├── ViewModels/
│   ├── Base/
│   │   ├── ViewModelBase.cs
│   │   ├── RelayCommand.cs
│   │   └── AsyncRelayCommand.cs
│   │
│   ├── MainViewModel.cs
│   └── ZipTreeNodeViewModel.cs
│
└── Helpers/
    └── FileSizeHelper.cs
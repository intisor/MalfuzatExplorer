<p align="center">
  <img src="https://img.icons8.com/?size=512&id=55494&format=png" width="20%" alt="MALFUZATEXPLORER-logo">
</p>
<p align="center">
    <h1 align="center">MALFUZAT EXPLORER</h1>
</p>
<p align="center">
    <em>Advanced Search and Exploration of Islamic Spiritual Teachings</em>
</p>

<p align="center">
    <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet" alt=".NET 8.0">
    <img src="https://img.shields.io/badge/ASP.NET_Core-MVC-512BD4?style=flat&logo=dotnet" alt="ASP.NET Core MVC">
    <img src="https://img.shields.io/badge/iText7-8.0.5-E74C3C?style=flat" alt="iText7">
    <img src="https://img.shields.io/badge/Bootstrap-5.x-7952B3?style=flat&logo=bootstrap" alt="Bootstrap">
    <img src="https://img.shields.io/badge/License-Open_Source-green?style=flat" alt="License">
</p>

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Features](#-features)
- [Technology Stack](#-technology-stack)
- [Architecture](#-architecture)
- [Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Configuration](#configuration)
  - [Running the Application](#running-the-application)
- [Usage Guide](#-usage-guide)
- [Project Structure](#-project-structure)
- [Core Components](#-core-components)
- [Development Guide](#-development-guide)
- [Deployment](#-deployment)
- [Contributing](#-contributing)
- [Roadmap](#-roadmap)
- [Contact](#-contact)

---

## 🌟 Overview

**Malfuzat Explorer** is an advanced web application designed to facilitate comprehensive search and exploration of **Malfuzat** - the compilation of the sayings and discourses of Hazrat Mirza Ghulam Ahmad (AS), the Promised Messiah and founder of the Ahmadiyya Muslim Community.

### Purpose

This application serves multiple vital purposes:

- **🔍 Research Tool**: Enables scholars, researchers, and students to efficiently search through extensive volumes of Malfuzat
- **📚 Spiritual Growth**: Provides easy access to Islamic teachings for personal development and understanding
- **🌐 Preservation**: Helps preserve and disseminate important spiritual and religious literature
- **🔎 Advanced Discovery**: Offers contextual search capabilities across multiple PDF volumes with intelligent text extraction

### What is Malfuzat?

Malfuzat (Arabic: مَلْفُوظَات, literally "utterances" or "sayings") is a ten-volume collection documenting the discourses, teachings, and spiritual insights of Hazrat Mirza Ghulam Ahmad (AS). These volumes contain valuable religious guidance, interpretations of Islamic teachings, and spiritual wisdom delivered during various gatherings and conversations.

---

## ✨ Features

### Core Functionality

- **🔎 Full-Text PDF Search**: Comprehensive search across 8 volumes of Malfuzat PDF documents (Volumes 1-4, 7-10)
- **🎯 Contextual Results**: Displays search results with surrounding context (up to 200 words) for better understanding
- **📖 Page Reference**: Identifies exact volume and page number ("leaf") where matches are found
- **🔆 Keyword Highlighting**: Visual emphasis on search terms within results using yellow highlighting

### Multilingual Support

- **🌐 Arabic & Urdu Text Rendering**: Specialized font support using 'Noto Nastaliq Urdu' and 'Amiri' fonts
- **📝 Right-to-Left (RTL) Display**: Automatic detection and proper rendering of Arabic/Urdu text with RTL directionality
- **💚 Special Language Styling**: Arabic and Urdu text rendered in lime green with bold formatting for enhanced readability

### User Experience

- **⚡ Loading Indicator**: Visual preloader with animated GIF during page loads
- **📱 Responsive Design**: Bootstrap-based responsive layout that works on desktop, tablet, and mobile devices
- **🎨 Alternating Result Rows**: Color-coded even/odd rows for improved result scanning
- **✅ Input Validation**: Client and server-side validation to ensure valid search queries
- **❌ Error Handling**: Graceful error messages when no results are found or errors occur

### Technical Features

- **🚀 Async Processing**: Asynchronous search operations for better performance
- **📄 iText7 Integration**: Professional PDF text extraction using iText7 library (v8.0.5)
- **🔄 Case-Insensitive Search**: Intelligent search that matches regardless of letter casing
- **🧩 Regex-based Text Processing**: Advanced text processing for accurate highlighting and language detection

---

## 🛠 Technology Stack

### Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 8.0 | Core runtime framework |
| **ASP.NET Core MVC** | 8.0 | Web application framework with Model-View-Controller pattern |
| **C#** | 12.0 | Primary programming language |
| **iText7** | 8.0.5 | PDF text extraction and processing |
| **NEST** | 7.17.5 | Elasticsearch .NET client (prepared for future enhancements) |

### Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| **Bootstrap** | 5.x | Responsive UI framework |
| **jQuery** | 3.x | JavaScript library for DOM manipulation |
| **jQuery Validation** | 1.x | Client-side form validation |
| **Custom CSS** | - | Application-specific styling |

### Development Tools

- **Visual Studio 2022** - Primary IDE (Version 17.10+)
- **Git** - Version control
- **GitHub Actions** - CI/CD pipeline
- **Azure Web Apps** - Cloud hosting platform

---

## 🏗 Architecture

### Application Architecture

The application follows the **Model-View-Controller (MVC)** architectural pattern:

```
┌─────────────────────────────────────────────────────────┐
│                      Client Browser                      │
│           (HTML/CSS/JavaScript + Bootstrap)              │
└────────────────────┬────────────────────────────────────┘
                     │ HTTP Request/Response
                     ▼
┌─────────────────────────────────────────────────────────┐
│                   ASP.NET Core MVC                       │
│  ┌──────────────────────────────────────────────────┐  │
│  │              Controllers Layer                    │  │
│  │         (HomeController - Routes/Logic)           │  │
│  └──────────────┬───────────────────────────────────┘  │
│                 │                                        │
│  ┌──────────────▼───────────────────────────────────┐  │
│  │               Models Layer                        │  │
│  │      (MalfuzatModel, ErrorViewModel)             │  │
│  └──────────────┬───────────────────────────────────┘  │
│                 │                                        │
│  ┌──────────────▼───────────────────────────────────┐  │
│  │               Views Layer                         │  │
│  │         (Razor CSHTML Templates)                  │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────┬───────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│              PDF Processing Layer                        │
│         (iText7 - PdfReader/PdfDocument)                │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│              Static PDF Files                            │
│        (wwwroot/Malfuzat/*.pdf - 8 volumes)             │
└─────────────────────────────────────────────────────────┘
```

### Search Flow

1. **User Input**: User enters search query in the web interface
2. **Request Handling**: HomeController receives POST request via Search action
3. **PDF Processing**: 
   - Iterates through 8 PDF files in parallel
   - Uses iText7 PdfReader to extract text from each page
   - Performs case-insensitive text matching
4. **Context Extraction**: Extracts 100 words before and after the match
5. **Text Enhancement**:
   - Highlights search terms with `<mark>` tags
   - Detects Arabic/Urdu text using regex pattern matching Unicode ranges
   - Wraps special language text in RTL spans
6. **Response**: Returns formatted results to the view for display

---

## 🚀 Getting Started

### Prerequisites

Before running this application, ensure you have the following installed:

- **.NET 8.0 SDK or later** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
  ```bash
  dotnet --version  # Should show 8.0.x or higher
  ```

- **Visual Studio 2022** (recommended) or **Visual Studio Code** with C# extension
  - Visual Studio 2022 version 17.8 or later recommended
  
- **Git** for version control ([Download](https://git-scm.com/downloads))

- **Web Browser** - Modern browser (Chrome, Firefox, Edge, Safari)

### Installation

#### Step 1: Clone the Repository

```bash
git clone https://github.com/intisor/MalfuzatExplorer.git
cd MalfuzatExplorer
```

#### Step 2: Restore NuGet Packages

```bash
dotnet restore
```

This will install the required dependencies:
- iText7 (8.0.5)
- NEST (7.17.5)
- ASP.NET Core framework packages

#### Step 3: Verify PDF Files

Ensure the Malfuzat PDF files are present in `wwwroot/Malfuzat/`:
- Malfuzat-1.pdf
- Malfuzat-2.pdf
- Malfuzat-3.pdf
- Malfuzat-4.pdf
- Malfuzat-7.pdf
- Malfuzat-8.pdf
- Malfuzat-9.pdf
- Malfuzat-10.pdf

**Note**: If PDF files are not included in the repository, you need to obtain them separately and place them in the `wwwroot/Malfuzat/` directory.

### Configuration

The application uses standard ASP.NET Core configuration files:

#### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

#### appsettings.Development.json

Used for development-specific settings. Logging levels can be adjusted here for debugging.

### Running the Application

#### Option 1: Using .NET CLI

```bash
# Development mode with hot reload
dotnet run

# Or specify the project explicitly
dotnet run --project MalfuzatExplorer.csproj
```

The application will start and be accessible at:
- **HTTP**: http://localhost:5278
- **HTTPS**: https://localhost:7163

#### Option 2: Using Visual Studio

1. Open `MalfuzatExplorer.sln` in Visual Studio 2022
2. Select the desired profile from the dropdown (http, https, or IIS Express)
3. Press **F5** to run with debugging, or **Ctrl+F5** to run without debugging

#### Option 3: Using IIS Express

```bash
dotnet run --launch-profile "IIS Express"
```

### Building for Production

```bash
# Build the project
dotnet build --configuration Release

# Publish the application
dotnet publish --configuration Release --output ./published
```

The published files will be in the `./published` directory, ready for deployment.

---

## 📖 Usage Guide

### Basic Search

1. **Navigate** to the home page (default route: `/`)
2. **Enter** your search query in the text input field
   - Queries can be in English, Arabic, or Urdu
   - Search is case-insensitive
3. **Click** the "Search" button to execute the search
4. **View** the results displayed below the search form

### Understanding Search Results

Each search result displays:
- **Volume Identification**: Shows which Malfuzat volume contains the match (e.g., "Found in Malfuzat-1")
- **Page Number**: Exact page (leaf) number where the text appears
- **Context**: Surrounding text (up to 100 words before and after) for context
- **Highlighted Query**: Your search term highlighted in yellow
- **Language Formatting**: Arabic/Urdu text displayed in green with RTL formatting

### Search Examples

**Example 1: English search**
```
Query: "prayer"
Result: "Found in Malfuzat-1 on leaf 45: ...context around the word prayer..."
```

**Example 2: Arabic/Urdu search**
```
Query: "نماز"
Result: Multiple results showing Arabic/Urdu text in green RTL formatting
```

### Tips for Effective Searching

- **Use specific terms**: More specific queries yield more relevant results
- **Try variations**: Search for related terms if initial search yields no results
- **Short queries**: Single words or short phrases work best
- **Language mixing**: You can search for Arabic/Urdu terms even in English interface

---

## 📁 Project Structure

```
MalfuzatExplorer/
├── Controllers/
│   └── HomeController.cs           # Main controller handling search logic
├── Models/
│   ├── MalfuzatModel.cs           # Model for search query and results
│   └── ErrorViewModel.cs           # Model for error handling
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml           # Main search interface
│   │   └── Privacy.cshtml         # Privacy policy page
│   ├── Shared/
│   │   ├── _Layout.cshtml         # Main layout template
│   │   ├── _Layout.cshtml.css     # Layout-specific styles
│   │   ├── Error.cshtml           # Error page
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── _ViewImports.cshtml        # Global view imports
│   └── _ViewStart.cshtml          # View start configuration
├── wwwroot/
│   ├── Malfuzat/                  # PDF files directory
│   │   ├── Malfuzat-1.pdf
│   │   ├── Malfuzat-2.pdf
│   │   ├── Malfuzat-3.pdf
│   │   ├── Malfuzat-4.pdf
│   │   ├── Malfuzat-7.pdf
│   │   ├── Malfuzat-8.pdf
│   │   ├── Malfuzat-9.pdf
│   │   └── Malfuzat-10.pdf
│   ├── css/
│   │   └── site.css               # Custom application styles
│   ├── js/
│   │   └── site.js                # Custom JavaScript
│   ├── lib/                       # Client-side libraries
│   │   ├── bootstrap/             # Bootstrap framework
│   │   ├── jquery/                # jQuery library
│   │   └── jquery-validation/     # jQuery validation
│   ├── favicon.ico                # Application icon
│   ├── favico.ico
│   └── preloader.gif              # Loading animation
├── Properties/
│   └── launchSettings.json        # Launch configuration profiles
├── .github/
│   └── workflows/
│       └── MalfuzatExplorer.yml   # CI/CD pipeline configuration
├── Program.cs                      # Application entry point
├── MalfuzatExplorer.csproj        # Project file
├── MalfuzatExplorer.sln           # Solution file
├── appsettings.json               # Application configuration
├── appsettings.Development.json   # Development configuration
├── .gitignore                     # Git ignore rules
└── README.md                      # This file
```

---

## 🔧 Core Components

### Models

#### MalfuzatModel
```csharp
public class MalfuzatModel
{
    public string Query { get; set; }              // User's search query
    public List<string> Results { get; set; }      // List of search results
    public int PageNumber { get; set; }            // For pagination (future use)
}
```

#### ErrorViewModel
```csharp
public class ErrorViewModel
{
    public string? RequestId { get; set; }         // Request tracking ID
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
```

### Controllers

#### HomeController

**Key Methods:**

1. **Index()** - `GET /`
   - Displays the main search interface
   - Initializes empty MalfuzatModel

2. **Search(MalfuzatModel model)** - `POST /Home/Search`
   - Receives search query from form
   - Validates input
   - Calls SearchPdfForQueryAsync()
   - Applies language formatting
   - Returns results to view

3. **SearchPdfForQueryAsync(string query)**
   - Iterates through 8 PDF files
   - Opens each PDF using iText7 PdfReader
   - Extracts text from every page
   - Searches for query (case-insensitive)
   - Collects matching results with context
   - Returns formatted result list

4. **GetContextAroundQueryAsync(string content, string query)**
   - Splits page content into words
   - Finds query position
   - Extracts 100 words before and after match
   - Returns contextual snippet

5. **HighlightQueryAsync(string text, string query)**
   - Uses regex to find query matches
   - Wraps matches in `<mark>` HTML tags
   - Returns highlighted text

6. **SpecialLanguageAsync(string result, string query)**
   - Detects Arabic/Urdu characters (Unicode ranges U+0600-U+06FF, U+0750-U+077F, U+FB50-U+FDFF, U+FE70-U+FEFF)
   - Wraps detected text in RTL spans with special styling
   - Returns formatted result

### Views

#### Index.cshtml
- Main search interface
- Form with text input and search button
- Results display area with alternating row colors
- Embedded CSS for custom styling
- Special font support for Arabic/Urdu

#### _Layout.cshtml
- Master page layout
- Navigation bar
- Preloader animation
- Footer
- Includes Bootstrap, jQuery, and validation scripts

---

## 💻 Development Guide

### Development Environment Setup

1. **Clone and Restore**
   ```bash
   git clone https://github.com/intisor/MalfuzatExplorer.git
   cd MalfuzatExplorer
   dotnet restore
   ```

2. **Open in IDE**
   - Visual Studio: Open `MalfuzatExplorer.sln`
   - VS Code: Open folder and install C# extension

3. **Run in Development Mode**
   ```bash
   dotnet run --environment Development
   ```

### Project Dependencies

**NuGet Packages:**
```xml
<PackageReference Include="itext7" Version="8.0.5" />
<PackageReference Include="NEST" Version="7.17.5" />
```

### Adding New Features

#### Adding a New PDF Volume

1. Place PDF file in `wwwroot/Malfuzat/`
2. Update the `pdfFiles` array in `HomeController.cs`:
   ```csharp
   private string[] pdfFiles = new string[]
   {
       // ... existing files ...
       Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Malfuzat", "Malfuzat-11.pdf"),
   };
   ```

#### Modifying Search Logic

The search logic is centralized in `SearchPdfForQueryAsync()`. Key areas to modify:

- **Search Algorithm**: Line 82 - `pageText.Contains(query, ...)`
- **Context Size**: Line 108 - Change `100` to adjust word count
- **Highlighting**: `HighlightQueryAsync()` method

### Building and Testing

```bash
# Build the project
dotnet build

# Run tests (if available)
dotnet test

# Clean build artifacts
dotnet clean
```

### Code Style

- Follow C# coding conventions
- Use async/await for I/O operations
- Implement proper error handling with try-catch
- Add XML documentation comments for public methods

---

## 🚢 Deployment

### Azure Web Apps Deployment

The project includes automated CI/CD via GitHub Actions.

#### Automated Deployment

The `.github/workflows/MalfuzatExplorer.yml` workflow automatically:
1. Triggers on push to `master` branch
2. Builds the project with .NET 8.0
3. Runs tests
4. Publishes artifacts
5. Deploys to Azure Web App

#### Manual Deployment to Azure

```bash
# Publish the application
dotnet publish --configuration Release --output ./published

# Deploy to Azure (using Azure CLI)
az webapp up --name MalfuzatExplorer --resource-group YourResourceGroup
```

#### Deployment Configuration

**Environment Variables:**
- `ASPNETCORE_ENVIRONMENT`: Set to "Production"
- `DOTNET_CORE_VERSION`: 8.0.x

**Azure Web App Settings:**
- Runtime: .NET 8
- Platform: Windows or Linux
- Always On: Enabled (recommended)

### Other Hosting Options

#### IIS Deployment

1. Publish the application
2. Configure IIS with .NET Core hosting bundle
3. Create a new site pointing to published folder
4. Configure application pool for "No Managed Code"

#### Docker Deployment

Create a `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MalfuzatExplorer.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MalfuzatExplorer.dll"]
```

Build and run:
```bash
docker build -t malfuzatexplorer .
docker run -p 8080:80 malfuzatexplorer
```

---

## 🤝 Contributing

Contributions are welcome and greatly appreciated! Here's how you can contribute to Malfuzat Explorer:

### Ways to Contribute

- **🐛 Report Bugs**: Submit detailed bug reports via [GitHub Issues](https://github.com/intisor/MalfuzatExplorer/issues)
- **💡 Feature Requests**: Suggest new features or enhancements
- **📝 Documentation**: Improve documentation, add examples, or fix typos
- **🔧 Code Contributions**: Submit pull requests with bug fixes or new features
- **💬 Community Support**: Help answer questions in [Discussions](https://github.com/intisor/MalfuzatExplorer/discussions)

### Contribution Workflow

1. **Fork the Repository**
   ```bash
   # Fork via GitHub UI, then clone your fork
   git clone https://github.com/YOUR-USERNAME/MalfuzatExplorer.git
   cd MalfuzatExplorer
   ```

2. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/your-bugfix-name
   ```

3. **Make Your Changes**
   - Write clean, documented code
   - Follow existing code style
   - Add comments where necessary
   - Test your changes thoroughly

4. **Commit Your Changes**
   ```bash
   git add .
   git commit -m "feat: Add your descriptive commit message"
   ```
   
   **Commit Message Guidelines:**
   - `feat:` New feature
   - `fix:` Bug fix
   - `docs:` Documentation changes
   - `style:` Code style changes (formatting, etc.)
   - `refactor:` Code refactoring
   - `test:` Adding tests
   - `chore:` Maintenance tasks

5. **Push to Your Fork**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Create a Pull Request**
   - Go to the original repository on GitHub
   - Click "New Pull Request"
   - Select your branch
   - Provide a clear description of your changes
   - Reference any related issues

### Development Guidelines

- **Code Quality**: Ensure code is clean, readable, and well-documented
- **Testing**: Test all changes locally before submitting
- **Dependencies**: Minimize new dependencies; discuss major additions first
- **Performance**: Consider performance implications of changes
- **Security**: Never commit sensitive data or credentials

### Code Review Process

1. Maintainers will review your PR
2. Address any requested changes
3. Once approved, your PR will be merged
4. Your contribution will be recognized in release notes

---

## 🗺 Roadmap

### Completed

- [x] **PDF Text Extraction**: Implemented iText7 library for document processing
- [x] **Search Functionality**: Core search logic with contextual results
- [x] **Multilingual Support**: Arabic and Urdu text rendering with RTL support
- [x] **UI Design**: Responsive Bootstrap-based interface
- [x] **Azure Deployment**: CI/CD pipeline and cloud hosting

### In Progress

- [ ] **Performance Optimization**: Caching frequently accessed PDF content
- [ ] **Advanced Search**: Boolean operators (AND, OR, NOT) and phrase search
- [ ] **Search History**: Track and display recent searches

### Future Enhancements

#### Short-term (Next 3-6 months)

- [ ] **Pagination**: Display results across multiple pages
- [ ] **Export Results**: Download search results as PDF or text
- [ ] **Bookmarking**: Save favorite passages for later reference
- [ ] **Improved UI/UX**: Enhanced visual design and user experience
- [ ] **Mobile Optimization**: Better mobile-specific interface

#### Medium-term (6-12 months)

- [ ] **Elasticsearch Integration**: Leverage NEST library for faster, more sophisticated searching
- [ ] **Full-text Indexing**: Pre-index all PDFs for instant search results
- [ ] **Advanced Filtering**: Filter by volume, date range, or topic
- [ ] **Multi-language Interface**: UI in multiple languages (English, Urdu, Arabic)
- [ ] **User Accounts**: Personal libraries and saved searches
- [ ] **API Development**: RESTful API for third-party integrations

#### Long-term (12+ months)

- [ ] **AI-Powered Features**:
  - Semantic search using natural language processing
  - Topic modeling and automatic categorization
  - Translation assistance
- [ ] **Community Features**:
  - User annotations and notes
  - Discussion forums
  - Collaborative study groups
- [ ] **Additional Content**: 
  - Include other Islamic literature
  - Cross-reference with related texts
  - Audio/video content integration
- [ ] **Mobile Apps**: Native iOS and Android applications

### Community Suggestions

We welcome suggestions for the roadmap! Please share your ideas in [GitHub Discussions](https://github.com/intisor/MalfuzatExplorer/discussions).

---

## 📧 Contact

**Developer**: Abdul Awwal Intisor

- **Email**: [abdulawwalintisor777@gmail.com](mailto:abdulawwalintisor777@gmail.com)
- **LinkedIn**: [linkedin.com/in/intitech07](https://www.linkedin.com/in/intitech07)
- **GitHub**: [@intisor](https://github.com/intisor)

**Project Repository**: [https://github.com/intisor/MalfuzatExplorer](https://github.com/intisor/MalfuzatExplorer)

**Issues & Bug Reports**: [GitHub Issues](https://github.com/intisor/MalfuzatExplorer/issues)

**Feature Requests**: [GitHub Discussions](https://github.com/intisor/MalfuzatExplorer/discussions)

---

## 📄 License

This project is open-source and available for use in accordance with standard open-source practices. Please refer to the repository for specific license details.

## 🙏 Acknowledgments

- **Ahmadiyya Muslim Community**: For the compilation and preservation of Malfuzat
- **iText7 Team**: For the excellent PDF processing library
- **ASP.NET Core Team**: For the robust web framework
- **Bootstrap Team**: For the responsive UI framework
- **All Contributors**: Thank you to everyone who contributes to this project

---

## 🌐 Additional Resources

- **ASP.NET Core Documentation**: [https://docs.microsoft.com/aspnet/core](https://docs.microsoft.com/aspnet/core)
- **iText7 Documentation**: [https://itextpdf.com/products/itext-7](https://itextpdf.com/products/itext-7)
- **Bootstrap Documentation**: [https://getbootstrap.com/docs](https://getbootstrap.com/docs)
- **Ahmadiyya Muslim Community**: [https://www.alislam.org](https://www.alislam.org)

---

<p align="center">
  <sub>Built with ❤️ for the exploration and preservation of Islamic spiritual knowledge</sub>
</p>

<p align="center">
   <a href="https://github.com/intisor/MalfuzatExplorer/graphs/contributors">
      <img src="https://contrib.rocks/image?repo=intisor/MalfuzatExplorer" alt="Contributors">
   </a>
</p>

<p align="center">
  <em>May this tool benefit all seekers of knowledge and spiritual growth</em>
</p>

---

**Last Updated**: December 2024

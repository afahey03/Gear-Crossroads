# Gear Crossroads
I made this as an excuse to learn Vue.js.

Gear Crossroads is a web platform designed for enthusiasts to create, share, and manage equipment setups for various hobbies and activities. Whether you're into photography, music, gaming, or any gear-intensive pursuit, Gear Crossroads provides a collaborative space to showcase your setups, discover others' configurations, and connect with like-minded users.

## Purpose

- **Showcase Setups:** Users can create detailed setups, including descriptions, images, and associated items.
- **Community Sharing:** Explore and view setups created by other users, fostering inspiration and knowledge sharing.
- **Profile Management:** Each user has a customizable profile with display name and avatar upload.
- **Item Management:** Add, edit, and remove items from setups, with the ability to manage item details.
- **Modern UX:** Features a clean, responsive interface with custom alerts, confirmation dialogs, and instant feedback for actions.

## Tech Stack

### Frontend
- **Vue 3**
- **TypeScript**
- **Pinia**
- **Vue Router**
- **Axios**
- **Tailwind CSS**
- **Vite**

### Backend
- **ASP.NET Core (C#)**
- **Entity Framework Core**
- **MySQL**
- **Identity & JWT Authentication**
- **Static File Serving**

### Project Structure
- `gear-crossroads-ui/`: Vue 3 frontend application
  - `src/components/`: Reusable UI components (NavBar, CustomAlert, ConfirmDialog, etc.)
  - `src/views/`: Page-level views (Home, Login, Register, Setups, Setup Detail, etc.)
  - `src/stores/`: Pinia stores for state management (user, alert, etc.)
  - `src/assets/`: Static assets (logo, favicon, etc.)
- `GearCrossroads.Api/`: ASP.NET Core backend API
  - `Controllers/`: API endpoints for authentication, setups, items
  - `Models/`: Entity and DTO definitions
  - `Data/`: Database context and migrations
  - `wwwroot/avatars/`: User-uploaded avatar images

## Features
- User registration, login, and JWT-based authentication
- Profile editing with avatar upload and display name
- Create, view, and delete setups
- Add, remove, and manage items within setups

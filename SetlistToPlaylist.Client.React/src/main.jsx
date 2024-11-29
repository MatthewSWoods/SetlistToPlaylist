import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import Setlisttoplaylist from "./setlisttoplaylist.jsx";

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <Setlisttoplaylist />
  </StrictMode>)